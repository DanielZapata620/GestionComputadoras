using Servidor.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection.Metadata;
using System.Text;
using System.Text.Json;
using System.Windows.Threading;


namespace Servidor.Services;

public class ServidorService
{
    public List<Computadora> ListaComputadoras { get; set; } = new();
    public List<string> ListaLaboratorios { get; set; } = new();

    public Computadora UltimaComputadora { get; set; } = new();

    DispatcherTimer timerStatus;

    public ServidorService()
    {
        timerStatus = new DispatcherTimer();
        timerStatus.Interval = TimeSpan.FromSeconds(30);
        timerStatus.Tick += TimerStatusTick;
        timerStatus.Start();
    }
    public UdpClient Servidor { get; set; }

    int puerto = 10200;

    public event Action<string>? ComputadoraRegistrada;
    public event Action<List<Computadora>>? VerificarConexion;
    public event Action? ActualizarListaComputadoras;
    public event Action? ActualizarListaLaboratorios;


    public void IniciarServidor()
    {
        IPEndPoint serverEP = new(IPAddress.Any, puerto);
        Servidor = new UdpClient(serverEP);

        Thread hilo = new(RecibirMensajes);
        hilo.IsBackground = true;
        hilo.Start();

        ListaComputadoras.Clear();
        ListaComputadoras = LeerJson();
        VerificarStatusGlobalBroadcast(true);

        ActualizarListaComputadoras?.Invoke();

        ObtenerLaboratorios();

    }

    public void RecibirMensajes()
    {
        while (true)
        {
            try
            {
                IPEndPoint clientEP = new(IPAddress.None, 0);


                byte[] buffer = Servidor.Receive(ref clientEP);
                string comando = Encoding.UTF8.GetString(buffer);

                string[] comandoSeparado = comando.Split('|');

                if (comandoSeparado[0] == "REGISTRAR" && comandoSeparado.Length > 1)
                {
                    if (ListaComputadoras.Any(x => x.Identificador == comandoSeparado[1]))
                    {

                        //var comandoEnviar = $"RECHAZAR";
                        //var compuEncontrada = ListaComputadoras.FirstOrDefault(x => x.Identificador == comandoSeparado[1]);
                        //EnviarMensaje(comandoEnviar, compuEncontrada);

                        var compuEncontrada = ListaComputadoras.FirstOrDefault(x => x.Identificador == comandoSeparado[1]);

                        if (compuEncontrada != null)
                        {
                            compuEncontrada.NumLaboratorio = comandoSeparado[2];
                            compuEncontrada.NumPc = $"PC{comandoSeparado[3]}";
                            compuEncontrada.IP = clientEP.Address.ToString();
                            compuEncontrada.Puerto = clientEP.Port;
                            compuEncontrada.UltimaVez = DateTime.Now;
                            compuEncontrada.Encendida = true;
                            compuEncontrada.Conexion = comandoSeparado[4] == "True";
                            compuEncontrada.Histroial = false;

                            EnviarMensaje("APROBAR", compuEncontrada);

                            ComputadoraRegistrada?.Invoke(compuEncontrada.NumLaboratorio);
                            ActualizarListaComputadoras?.Invoke();

                            string json = JsonSerializer.Serialize(ListaComputadoras);
                            File.WriteAllText("computadoras.json", json);
                        }
                 

                    }
                    else
                    {

                        Computadora compu = new()
                        {
                            Identificador = comandoSeparado[1],
                            NumLaboratorio = $"{comandoSeparado[2]}",
                            NumPc = $"PC{comandoSeparado[3]}",
                            IP = clientEP.Address.ToString(),
                            Puerto = clientEP.Port,
                            FechaRegistro = DateOnly.FromDateTime(DateTime.Now),
                            UltimaVez = DateTime.Now,
                            Encendida = true,
                            Conexion = comandoSeparado[4] == "True" ? true : false,
                            Histroial = false,

                        };



                        var comandoEnviar = $"APROBAR";
                        EnviarMensaje(comandoEnviar, compu);

                        ListaComputadoras.Add(compu);
                        ComputadoraRegistrada?.Invoke(compu.NumLaboratorio);

                        string json = JsonSerializer.Serialize(ListaComputadoras);

                        File.WriteAllText("computadoras.json", json);


                    }

                }

                if (comandoSeparado[0] == "STATUSAPAGADOCOMPU" && comandoSeparado.Length > 1)
                {
                    var compuEncontrada = ListaComputadoras.FirstOrDefault(x => x.Identificador.ToUpper() == comandoSeparado[1]);
                    if (compuEncontrada != null)
                    {
                        compuEncontrada.Encendida = false;
                        compuEncontrada.Conexion = false;
                        compuEncontrada.UltimaVez = DateTime.Now;
                        ActualizarListaComputadoras?.Invoke();
                    }
                }
                    if (comandoSeparado[0] == "RESPUESTA" && comandoSeparado.Length > 1)
                {
                    var compuEncontrada = ListaComputadoras.FirstOrDefault(x => x.Identificador.ToUpper() == comandoSeparado[1]);
                    try
                    {

                        if (compuEncontrada != null)
                        {
                            compuEncontrada.IP = clientEP.Address.ToString();
                            compuEncontrada.Puerto = clientEP.Port;
                            compuEncontrada.Conexion = comandoSeparado[2] == "True" ? true : false;
                            compuEncontrada.Encendida = true;
                            compuEncontrada.UltimaVez = DateTime.Now;
                            compuEncontrada.Histroial = false;
                            ActualizarListaComputadoras?.Invoke();

                            ComputadoraRegistrada?.Invoke(compuEncontrada.NumLaboratorio);

                            string json = JsonSerializer.Serialize(ListaComputadoras);

                            File.WriteAllText("computadoras.json", json);

                        }

                       

                    }
                    catch (SocketException ex)
                    {
                        compuEncontrada.Conexion = false;
                        compuEncontrada.Encendida = false;
                        ActualizarListaComputadoras?.Invoke();
                    }

                }
            }
            catch (SocketException ex)
            {
                UltimaComputadora.Encendida = false;
                UltimaComputadora.Conexion = false;
                UltimaComputadora.UltimaVez = DateTime.Now;
              
                ActualizarListaComputadoras?.Invoke();
            }


         
        }

       

    }



    public void EnviarMensaje(string commando,  Computadora compu)
    {
        try 
        {
            IPAddress.TryParse(compu.IP, out IPAddress? ipServidor);
            IPEndPoint remoto = new IPEndPoint(ipServidor, compu.Puerto);

            byte[] buffer = Encoding.UTF8.GetBytes(commando);


            Servidor.Send(buffer, buffer.Length, remoto);
        }
        catch(SocketException)
        {
            compu.Encendida = false;
            compu.Conexion = false;
            ActualizarListaComputadoras?.Invoke();
        }
        

    }
    

    public void VerificarInternet(string Identificador)
    {
        var compuEncontrada = ListaComputadoras.FirstOrDefault(x => x.Identificador == Identificador && x.Encendida);
        if (compuEncontrada != null)
        {
            try
            {
                UltimaComputadora = compuEncontrada;
                //compuEncontrada.Encendida = false;
                //compuEncontrada.Conexion = false;
                EnviarMensaje("CONEXION", compuEncontrada);
            }
            catch (SocketException)
            {
                compuEncontrada.Encendida = false;
                compuEncontrada.Conexion = false;
                ActualizarListaComputadoras?.Invoke();
            }
        }
        ActualizarListaComputadoras?.Invoke();
    }

    public void ApagarComputadora(string Identificador)
    {
        var compuEncontrada = ListaComputadoras.FirstOrDefault(x => x.Identificador == Identificador && x.Encendida == true);
        if (compuEncontrada != null)
        {
            UltimaComputadora = compuEncontrada;
            EnviarMensaje("APAGAR", compuEncontrada);
            //ActualizarListaComputadoras?.Invoke();
        }
    }


    private List<Computadora> LeerJson()
    {
        if (File.Exists("computadoras.json"))
        {
            string json = File.ReadAllText("computadoras.json");
            return JsonSerializer.Deserialize<List<Computadora>>(json) ?? new List<Computadora>();
        }
        return new List<Computadora>();
    }

    public void VerificarStatusGlobal(bool Inicializar) 
    {
        foreach (var compu in ListaComputadoras)
        {
            //compu.Encendida = false;
            //compu.Conexion = false;
            //if(Inicializar==true){
            //    compu.Histroial = true;
            //}
            if (compu.UltimaVez.AddDays(15) < DateTime.Now)
            {
                compu.Histroial = true;
            }
            EnviarMensaje("STATUS", compu);
            ActualizarListaComputadoras?.Invoke();
        }
    }

    public void VerificarStatusGlobalBroadcast(bool Inicializar)
    {
        foreach (var compu in ListaComputadoras)
        {
            compu.Encendida = false;
            compu.Conexion = false;
            if (compu.UltimaVez.AddDays(15) < DateTime.Now)
            {
                compu.Histroial = true;
            }


        }
        
        Servidor.EnableBroadcast = true;
        IPEndPoint remoto = new IPEndPoint(IPAddress.Broadcast, 8888);
        string commando = "STATUS";

        byte[] buffer = Encoding.UTF8.GetBytes(commando);
        Servidor.Send(buffer, buffer.Length, remoto);

        ActualizarListaComputadoras?.Invoke();

    }
  
        
    

    public void ObtenerLaboratorios()
    {
        ListaComputadoras.Where(x=>x.Histroial==false).ToList().ForEach(x =>
        {
            if (!ListaLaboratorios.Contains(x.NumLaboratorio))
            {
                ListaLaboratorios.Add(x.NumLaboratorio);
            }
        });

        
        ActualizarListaLaboratorios?.Invoke();
        
    }

    public void filtrarComputadorasPorLaboratorio(string numLaboratorio)
    {
       
            var computadorasFiltradas = ListaComputadoras.Where(x => x.NumLaboratorio == numLaboratorio && x.Histroial==false).OrderBy(x => x.NumPc).ToList();
            VerificarConexion?.Invoke(computadorasFiltradas);

        
       
    }

    public void MostrarHistrial()
    {
        var computadorasFiltradas = ListaComputadoras.Where(x => x.Histroial == true).ToList();
        VerificarConexion?.Invoke(computadorasFiltradas);
    }


    private void TimerStatusTick(object? sender, EventArgs e)
    {
        // 1. Enviar STATUS a todas las máquinas
        VerificarStatusGlobal(false);

        // 2. Verificar timeout de respuesta (40s)
        foreach (var pc in ListaComputadoras)
        {


            if (pc.UltimaVez.AddSeconds(40) < DateTime.Now)
            {
                pc.Encendida = false;
                pc.Conexion = false;
            }

            if (pc.UltimaVez.AddDays(15) < DateTime.Now)
            {
                pc.Histroial = true;
            }

        }

        ActualizarListaComputadoras?.Invoke();
    }

    public void EliminarComputadora(string Identificador)
    {
        var compuEncontrada = ListaComputadoras.FirstOrDefault(x => x.Identificador == Identificador );
        if (compuEncontrada != null)
        {
            UltimaComputadora = compuEncontrada;
            ListaComputadoras.Remove(compuEncontrada);

            string json = JsonSerializer.Serialize(ListaComputadoras);
            File.WriteAllText("computadoras.json", json);

            ActualizarListaComputadoras?.Invoke();
        }
    }

}

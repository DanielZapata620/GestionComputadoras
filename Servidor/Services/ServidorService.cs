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


namespace Servidor.Services;

public class ServidorService
{
    public List<Computadora> ListaComputadoras { get; set; } = new();
    public List<string> ListaLaboratorios { get; set; } = new();

    public Computadora UltimaComputadora { get; set; } = new();



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
                        
                        var comandoEnviar = $"RECHAZAR";
                        var compuEncontrada = ListaComputadoras.FirstOrDefault(x => x.Identificador == comandoSeparado[1]);
                        EnviarMensaje(comandoEnviar, compuEncontrada);
                    }
                    else
                    {

                        Computadora compu = new()
                        {
                            NumLaboratorio = $"{comandoSeparado[2]}",
                            NumPc = $"PC{comandoSeparado[3]}",
                            IP = clientEP.Address.ToString(),
                            Puerto = clientEP.Port,
                            FechaRegistro = DateOnly.FromDateTime(DateTime.Now),
                            UltimaVez = DateOnly.FromDateTime(DateTime.Now),
                            Encendida = true,
                            Conexion= comandoSeparado[4] == "True" ? true : false,
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
                            compuEncontrada.UltimaVez = DateOnly.FromDateTime(DateTime.Now);
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
                UltimaComputadora.UltimaVez = DateOnly.FromDateTime(DateTime.Now);
              
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
                compuEncontrada.Encendida = false;
                compuEncontrada.Conexion = false;
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
            compuEncontrada.Encendida = false;
            compuEncontrada.Conexion = false;
            EnviarMensaje("APAGAR", compuEncontrada);
            ActualizarListaComputadoras?.Invoke();
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
            compu.Encendida = false;
            compu.Conexion = false;
            if(Inicializar==true){
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
            if (Inicializar == true)
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




}

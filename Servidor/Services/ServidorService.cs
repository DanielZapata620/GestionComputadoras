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
    //QUE EL NUMEOR LLEVE DOS DIGITOS
    public List<Computadora> ListaComputadoras { get; set; } = new();
    public List<string> ListaLaboratorios { get; set; } = new();

    public Computadora UltimaComputadora { get; set; } = new();



    public UdpClient Servidor { get; set; }

    int puerto = 10200;

    public event Action? ComputadoraRegistrada;
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
        VerificarStatusGlobal();

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
                        var error = "Eliga otro identifiacdor , ya que el que intenta usar ya se encuentra registrado";
                        var comandoEnviar = $"RECHAZAR|{error}";
                        EnviarMensaje(comandoEnviar, clientEP.Address.ToString(), clientEP.Port);
                    }
                    else
                    {

                        Computadora compu = new()
                        {
                            NumLaboratorio = $"LAB{comandoSeparado[2]}",
                            NumPc = $"PC{comandoSeparado[3]}",
                            IP = clientEP.Address.ToString(),
                            Puerto = clientEP.Port,
                            FechaRegistro = DateOnly.FromDateTime(DateTime.Now),
                            UltimaVez = DateOnly.FromDateTime(DateTime.Now),
                            Encendida = true,
                            Histroial = false,

                        };



                        var comandoEnviar = $"APROBAR";
                        EnviarMensaje(comandoEnviar, clientEP.Address.ToString(), clientEP.Port);

                        ListaComputadoras.Add(compu);
                        ComputadoraRegistrada?.Invoke();

                        string json = JsonSerializer.Serialize(ListaComputadoras);

                        File.WriteAllText("computadoras.json", json);


                    }

                }

                if (comandoSeparado[0] == "RESPUESTA" && comandoSeparado.Length > 1)
                {
                    var compuEncontrada = ListaComputadoras.FirstOrDefault(x => x.Identificador == comandoSeparado[1]);
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


                        }

                        ObtenerLaboratorios();
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
                //
                ActualizarListaComputadoras?.Invoke();
            }


            //if (comandoSeparado[0] == "REGISTRAR" && comandoSeparado.Length > 1)
        }

       

    }



    public void EnviarMensaje(string commando,  string ip, int port)
    {

        IPAddress.TryParse(ip, out IPAddress? ipServidor);
            IPEndPoint remoto = new IPEndPoint(ipServidor, port);
           
            byte[] buffer = Encoding.UTF8.GetBytes(commando);


            Servidor.Send(buffer, buffer.Length, remoto);

    }
        //if (commando == "RECHAZAR")
        //{


        //    IPEndPoint remoto = new IPEndPoint(ip, port);
        //    commando += "|" + parametro;
        //    byte[] buffer = Encoding.UTF8.GetBytes(commando);


        //    Servidor.Send(buffer, buffer.Length, remoto);

        //}

    


    public void VerificarInternet(string Identificador)
    {
        var compuEncontrada = ListaComputadoras.FirstOrDefault(x => x.Identificador == Identificador && x.Encendida);
        if (compuEncontrada != null)
        {
            try
            {
                UltimaComputadora = compuEncontrada;
                EnviarMensaje("CONEXION", compuEncontrada.IP, compuEncontrada.Puerto);
            }
            catch (SocketException)
            {
                compuEncontrada.Encendida = false;
                compuEncontrada.Conexion = false;
                ActualizarListaComputadoras?.Invoke();
            }
        }
    }

    public void ApagarComputadora(string Identificador)
    {
        var compuEncontrada = ListaComputadoras.FirstOrDefault(x => x.Identificador == Identificador && x.Encendida == true);
        if (compuEncontrada != null)
        {
            UltimaComputadora = compuEncontrada;
            compuEncontrada.Encendida = false;
            compuEncontrada.Conexion = false;
            EnviarMensaje("APAGAR", compuEncontrada.IP, compuEncontrada.Puerto);
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

    //public void DescubrirComputadorasBroadcast()
    //{
    //    Servidor.EnableBroadcast = true;
    //    EnviarMensaje("DESCUBRIR", "255.255.255.255", puerto);
    //}
    public void VerificarStatusGlobal() 
    {
        foreach (var compu in ListaComputadoras)
        {
            compu.Encendida = false;
            compu.Conexion = false;
            compu.Histroial = true;
            EnviarMensaje("STATUS", compu.IP, compu.Puerto);
        }
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
       
            var computadorasFiltradas = ListaComputadoras.Where(x => x.NumLaboratorio == numLaboratorio && x.Histroial==false ).ToList();
            VerificarConexion?.Invoke(computadorasFiltradas);

        
       
    }

    public void MostrarHistrial()
    {
        var computadorasFiltradas = ListaComputadoras.Where(x => x.Histroial == true).ToList();
        VerificarConexion?.Invoke(computadorasFiltradas);
    }




}

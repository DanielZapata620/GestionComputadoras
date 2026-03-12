using Servidor.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection.Metadata;
using System.Text;


namespace Servidor.Services;

public class ServidorService
{
    public List<Computadora> ListaComputadoras { get; set; } = new();

    public UdpClient Servidor { get; set; }

    int puerto = 10200;

    public event Action<Computadora>? ComputadoraRegistrada;
    public event Action<List<Computadora>>? VerificarConexion;


    public void IniciarServidor()
    {
        IPEndPoint serverEP = new(IPAddress.Any, puerto);
        Servidor = new UdpClient(serverEP);

        Thread hilo = new(RecibirMensajes);
        hilo.IsBackground = true;
        hilo.Start();

    }

    public void RecibirMensajes()
    {
        while (true)
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
                    EnviarMensaje(comandoEnviar, clientEP.Address, clientEP.Port);
                }
                else
                {

                    Computadora compu = new()
                    {
                        Identificador = comandoSeparado[1],
                        IP = clientEP.Address,
                        Puerto = clientEP.Port,
                        Encendida = true
                    };




                    ListaComputadoras.Add(compu);
                    ComputadoraRegistrada?.Invoke(compu);



                }

            }

            if(comandoSeparado[0] == "RESPUESTA" && comandoSeparado.Length > 1)
            {
                var compuEncontrada = ListaComputadoras.FirstOrDefault(x => x.Identificador == comandoSeparado[1] && x.Encendida == true);
                if (compuEncontrada != null)
                {
                    compuEncontrada.Conexion=comandoSeparado[2]=="True"?true:false;
                    VerificarConexion?.Invoke(ListaComputadoras);


                }
            }

            //if (comandoSeparado[0] == "REGISTRAR" && comandoSeparado.Length > 1)
        }
    }

   

    public void EnviarMensaje(string commando,  IPAddress ip, int port)
    {
       

            IPEndPoint remoto = new IPEndPoint(ip, port);
           
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
        var compuEncontrada = ListaComputadoras.FirstOrDefault(x => x.Identificador == Identificador && x.Encendida == true);
        if (compuEncontrada != null)
        {
            EnviarMensaje("CONEXION", compuEncontrada.IP, compuEncontrada.Puerto);
        }
    }

    public void ApagarComputadora(string Identificador)
    {
        var compuEncontrada = ListaComputadoras.FirstOrDefault(x => x.Identificador == Identificador && x.Encendida == true);
        if (compuEncontrada != null)
        {
            compuEncontrada.Encendida = false;
            compuEncontrada.Conexion = false;
            EnviarMensaje("APAGAR", compuEncontrada.IP, compuEncontrada.Puerto);
        }
    }









}

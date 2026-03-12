using Cliente.Models;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;

namespace Cliente.Service
{
    public class ClienteService
    {

        Computadora Computadora { get; set; }
        UdpClient Cliente = new();

        IPAddress ServerIp;
        int port = 10200;
        Ping ping = new();


        public event Action<string>? EnviarError;
        public event Action? Aprobado;

        private void RecibirMensajes()
        {
            while (true)
            {
                IPEndPoint clientEP = new(IPAddress.None, 0);


                byte[] buffer = Cliente.Receive(ref clientEP);
                string comando = Encoding.UTF8.GetString(buffer);

                string[] comandoSeparado = comando.Split('|');

                if (comandoSeparado[0] == "RECHAZAR" && comandoSeparado.Length > 1)
                {

                    var error = comandoSeparado[1];
                    EnviarError?.Invoke(error);


                }
                if (comandoSeparado[0] == "APROBADO")
                {
                    string json = JsonSerializer.Serialize(Computadora);
                    string rutaArchivo = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "computadora.json");
                    File.WriteAllText(rutaArchivo, json);
                    Aprobado?.Invoke();

                }
                if (comandoSeparado[0] == "CONEXION")
                {
                    PingReply respuesta = ping.Send("8.8.8.8", 3000);
                    bool Conexion;
                    if (respuesta.Status == IPStatus.Success)
                    {
                        Conexion = true;
                    }
                    else
                    {
                        Conexion = false;
                    }

                    var comandoRespuesta =$"RESPUESTA|{Computadora.Identificador}|{Conexion}";
                    EnviarMensaje(comandoRespuesta);
                }

                if (comandoSeparado[0] == "APAGAR")
                {

                    Process.Start("shutdown", "/s /t 0");
                }
                //Comando Aprobar para guardar el identificador y cambiar de vista 
            }
        }

        public void Conectar(string IpServidor, Computadora Compu)
        {

            if (IPAddress.TryParse(IpServidor, out IPAddress? ipServidor))
                Compu.Identificador = Compu.Identificador.Replace('|', '\0');
            //
            Computadora = Compu;
            ServerIp=ipServidor;

            var comando = $"REGISTRAR|{Compu.Identificador}";
            EnviarMensaje(comando);

            Thread hilo = new(RecibirMensajes);
            hilo.IsBackground = true;
            hilo.Start();
          
        }

        public void EnviarMensaje(string comando)
        {
            IPEndPoint remoto = new IPEndPoint(ServerIp, port);
            byte[] buffer = Encoding.UTF8.GetBytes(comando);
            Cliente.Send(buffer, buffer.Length, remoto);

        }
    }
}

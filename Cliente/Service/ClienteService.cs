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
        //Porperdad de registro pedniente para controlar las vistas
        Computadora Computadora { get; set; }
        UdpClient Cliente;

       

        IPAddress ServerIp;
        int port = 10200;
        Ping ping = new();


        public event Action<string>? EnviarError;
        public event Action<Computadora>? computadoraCargada;
        public event Action? Registrar;
        public event Action? Aprobado;
        public event Action? ServidorApagado;


        public void InicializarCliente()
        {

            Cliente = new();
            if (File.Exists("computadora.json"))
            {
                string json = File.ReadAllText("computadora.json");
                Computadora = JsonSerializer.Deserialize<Computadora>(json);
                computadoraCargada?.Invoke(Computadora);

            }
            else
            {
                Computadora = new Computadora();
            }

           
            

            // CASO 1: no hay identificador
            if (string.IsNullOrEmpty(Computadora.Identificador))
            {

                Registrar?.Invoke();
                return;
            }
            ServerIp = IPAddress.Parse(Computadora.IpServidor);
            // CASO 2: tiene identificador pero no está registrada
            if (!Computadora.RegistradaEnELServidor)
            {
               
                Conectar(ServerIp.ToString(), Computadora);
                return;
            }

            // CASO 3: ya está registrada
            if (Computadora.RegistradaEnELServidor)
            {

                
                bool Conexion = PingDNS();

                var comandoRespuesta = $"RESPUESTA|{Computadora.Identificador}|{Conexion}";
                EnviarMensaje(comandoRespuesta);

                Thread hilo = new(RecibirMensajes);
                hilo.IsBackground = true;
                hilo.Start();

                Aprobado?.Invoke(); 

            }
        }

        private void RecibirMensajes()
        {
            while (true)
            {
                try
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
                    if (comandoSeparado[0] == "APROBAR")
                    {
                        Computadora.RegistradaEnELServidor = true;

                        string json = JsonSerializer.Serialize(Computadora);
                        File.WriteAllText("computadora.json", json);
                        Aprobado?.Invoke();

                    }
                    if (comandoSeparado[0] == "CONEXION")
                    {
                        bool Conexion = PingDNS();

                        var comandoRespuesta = $"RESPUESTA|{Computadora.Identificador}|{Conexion}";
                        EnviarMensaje(comandoRespuesta);
                    }

                    if (comandoSeparado[0] == "APAGAR")
                    {

                        Process.Start("shutdown", "/s /t 0");
                    }

                    if (comandoSeparado[0] == "STATUS")
                    {
                        //VERIFICAR
                        bool Conexion = PingDNS();

                        var comandoRespuesta = $"RESPUESTA|{Computadora.Identificador}|{Conexion}";
                        EnviarMensaje(comandoRespuesta);
                    }

                }
                catch (SocketException)
                {
                    string json = JsonSerializer.Serialize(Computadora);
                    File.WriteAllText("computadora.json", json);
                    ServidorApagado?.Invoke();
                }


                //Comando Aprobar para guardar el identificador y cambiar de vista 
            }
        }

        private bool PingDNS()
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

            return Conexion;
        }

        public void Conectar(string IpServidor, Computadora Compu)
        {

            if (IPAddress.TryParse(IpServidor, out IPAddress? ipServidor))
                //Compu.Identificador = Compu.Identificador.Replace('|', '\0');
            //
            Computadora = Compu;
            Compu.Identificador= $"LAB{Compu.LAB}-PC{Compu.PC}";
            ServerIp =ipServidor;
            Compu.IpServidor=ServerIp.ToString();

            //Cliente.Client.ReceiveTimeout = 10000;

            
            var comando = $"REGISTRAR|{Compu.Identificador}|{Compu.LAB}|{Compu.PC}";
            EnviarMensaje(comando);

            Thread hilo = new(RecibirMensajes);
            hilo.IsBackground = true;
            hilo.Start();

            

          


            

        }

        public void EnviarMensaje(string comando)
        {
            try
            {
                IPEndPoint remoto = new IPEndPoint(ServerIp, port);
                byte[] buffer = Encoding.UTF8.GetBytes(comando);
                Cliente.Send(buffer, buffer.Length, remoto);
            }
            catch (SocketException)
            {
               
                ServidorApagado?.Invoke();
            }
        
           

        }

       
    }
}

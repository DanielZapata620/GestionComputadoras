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
        UdpClient Cliente=new(8888);

       

        IPAddress ServerIp;
        int port = 10200;
        Ping ping = new();


        public event Action EnviarError;
        public event Action<Computadora>? computadoraCargada;
        public event Action? Registrar;
        public event Action? Aprobado;
        public event Action? ServidorApagado;


        public void InicializarCliente()
        {

            
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

           
            

    
            if (string.IsNullOrEmpty(Computadora.Identificador))
            {

                Registrar?.Invoke();
                return;
            }
            ServerIp = IPAddress.Parse(Computadora.IpServidor);
        
            if (!Computadora.RegistradaEnELServidor)
            {
               
                Conectar(ServerIp.ToString(), Computadora);
                ServidorApagado?.Invoke();
                return;
            }

       
            if (Computadora.RegistradaEnELServidor)
            {

                
                bool Conexion = PingDNS();

                var comandoRespuesta = $"RESPUESTA|{Computadora.Identificador}|{Conexion}";
                EnviarMensaje(comandoRespuesta);

                Thread hilo = new(RecibirMensajes);
                hilo.IsBackground = true;
                hilo.Start();
                ServidorApagado?.Invoke();

                //Aprobado?.Invoke(); 

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

                    if (comandoSeparado[0] == "RECHAZAR" && Computadora.RegistradaEnELServidor==false)
                    {

                        
                        EnviarError?.Invoke();


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
                        Aprobado?.Invoke();

                    }

                    if (comandoSeparado[0] == "APAGAR")
                    {

                        Process.Start("shutdown", "/s /t 0");
                    }

                    if (comandoSeparado[0] == "STATUS" )
                    {
                        if (Computadora.RegistradaEnELServidor == false)
                        {
                            bool internet = PingDNS();
                            var registrar = $"REGISTRAR|{Computadora.Identificador}|{Computadora.LAB.ToUpper()}|{Computadora.PC}|{internet}";
                            EnviarMensaje(registrar);
                        }
                        else
                        {
                            bool Conexion = PingDNS();

                            var comandoRespuesta = $"RESPUESTA|{Computadora.Identificador}|{Conexion}";
                            EnviarMensaje(comandoRespuesta);
                            Aprobado?.Invoke();
                        }
                        
                    }

                }
                catch (SocketException)
                {
                    string json = JsonSerializer.Serialize(Computadora);
                    File.WriteAllText("computadora.json", json);
                    ServidorApagado?.Invoke();
                }


            
            }
        }

        private bool PingDNS()
        {
            try
            {

                PingReply respuesta = ping.Send("8.8.8.8", 5000);
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
            catch
            {
                return false;
            }

        }

        public void Conectar(string IpServidor, Computadora Compu)
        {

            if (IPAddress.TryParse(IpServidor, out IPAddress? ipServidor))
                Compu.LAB = Compu.LAB.Replace('|', '\0');
                Compu.PC = Compu.PC.Replace('|', '\0');
            
            Computadora = Compu;
            Compu.Identificador= $"{Compu.LAB.ToUpper()}-PC{Compu.PC.ToUpper()}";
            ServerIp =ipServidor;
            Compu.IpServidor=ServerIp.ToString();

            //Cliente.Client.ReceiveTimeout = 10000;

            bool internet=PingDNS();
            
            var comando = $"REGISTRAR|{Compu.Identificador}|{Compu.LAB.ToUpper()}|{Compu.PC}|{internet}";
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

        public void Reconectar()
        {
            bool Conexion = PingDNS();

            var comandoRespuesta = $"RESPUESTA|{Computadora.Identificador}|{Conexion}";
            EnviarMensaje(comandoRespuesta);
        }

       
    }
}

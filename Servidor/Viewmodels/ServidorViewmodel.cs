using Servidor.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Servidor.Viewmodels
{
    public class ServidorViewmodel:INotifyPropertyChanged
    {
        public ObservableCollection<Computadora> ListaComputadoras { get; set; } = new();

        public UdpClient Servidor { get; set; }

        int puerto = 10200;

        public event PropertyChangedEventHandler? PropertyChanged;

        public ServidorViewmodel()
        {
            IPEndPoint serverEP = new(IPAddress.Any, puerto);
            Servidor = new UdpClient(serverEP);

            Thread hilo = new(RecibirMensajes);
            hilo.IsBackground=true;
            hilo.Start();

        }




        public void RecibirMensajes()
        {
            while(true){
                IPEndPoint clientEP = new(IPAddress.None, 0);


                byte[] buffer = Servidor.Receive(ref clientEP);
                string comando = Encoding.UTF8.GetString(buffer);

                string[] comandoSeparado = comando.Split('|');

                if (comandoSeparado[0] == "REGISTRAR" && comandoSeparado.Length > 1)
                {
                    if (ListaComputadoras.Any(x => x.Identificador == comandoSeparado[1]))
                    {
                        EnviarMensaje("RECHAZAR", "Eliga otro identifiacdor , ya que el que intenta usar ya se encuentra registrado", clientEP.Address, clientEP.Port);
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
                        
                    }
                       


                }           
            }
        }

        public void EnviarMensaje(string commando, string parametro,IPAddress ip,int port)
        {
            if (commando == "RECHAZAR")
            {
                
              
                IPEndPoint remoto = new IPEndPoint(ip, port);
                commando += "|"+parametro;
                byte[] buffer = Encoding.UTF8.GetBytes(commando);
                

                Servidor.Send(buffer, buffer.Length, remoto);

            }

        }
    }
}

using Cliente.Models;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Windows.Input;

namespace Cliente.Viewmodels
{
    public class ClienteViewmodel:INotifyPropertyChanged
    {
        public ICommand RegistrarCommand { get; set; }
        public Computadora Compu { get; set; } = new();

        public string Error { get; set; }

        public string IpServidor { get; set; } 
        
        UdpClient Cliente = new();
        int port = 10200;

        public event PropertyChangedEventHandler? PropertyChanged;

        public ClienteViewmodel()
        {
            RegistrarCommand = new RelayCommand(Conectar);
           

        }

        private void RecibirMensajes()
        {
            while (true)
            {
                IPEndPoint clientEP = new(IPAddress.None, 0);


                byte[] buffer = Cliente.Receive(ref clientEP);
                string comando = Encoding.UTF8.GetString(buffer);

                string[] comandoSeparado = comando.Split('|');

                if (comandoSeparado[0] == "RECHAZAR" && comandoSeparado.Length>1)
                {

                   Error=comandoSeparado[1];
                   PropertyChanged?.Invoke(this,new(nameof(Error)));


                }
            }
        }

        public void Conectar()
        {
            if(IPAddress.TryParse(IpServidor,out IPAddress? ipServidor))
            Compu.Identificador = Compu.Identificador.Replace('|', '\0');
            IPEndPoint remoto = new IPEndPoint(ipServidor, port);
            var comando = $"REGISTRAR|{Compu.Identificador}";
            byte[] buffer = Encoding.UTF8.GetBytes(comando);


            
            Cliente.Send(buffer, buffer.Length, remoto);

            Thread hilo = new(RecibirMensajes);
            hilo.IsBackground = true;
            hilo.Start();
        }
    }
}

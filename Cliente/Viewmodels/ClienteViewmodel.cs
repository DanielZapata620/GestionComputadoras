using Cliente.Models;
using Cliente.Service;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Windows.Input;
using System.Windows.Threading;

namespace Cliente.Viewmodels
{
    public class ClienteViewmodel: INotifyPropertyChanged
    {

        public ICommand RegistrarCommand { get; set; }
        public ICommand ReintentarCommand { get; set; }

        public string Error { get; set; }

        public Computadora Compu { get; set; } = new();

        public string VistaActual { get; set; }
        public string IpServidor { get; set; }
        ClienteService service = new();

        Dispatcher hiloUI;

        public event PropertyChangedEventHandler? PropertyChanged;

        public ClienteViewmodel()
        {
            hiloUI = Dispatcher.CurrentDispatcher;
            RegistrarCommand = new RelayCommand(Conectar);
            ReintentarCommand = new RelayCommand(Reintentar);
            service.Aprobado += Service_Aprobado;
            service.computadoraCargada += Service_computadoraCargada;
            service.ServidorApagado += Service_ServidorApagado;
            service.EnviarError += Service_EnviarError;
            
            service.InicializarCliente();

        }

        private void Service_EnviarError()
        {
            Error= "Eliga otro identificador ya que el que intenta usar ya se encuentra registrado";
        }

        private void Reintentar()
        {
            if (Compu.RegistradaEnELServidor == false)
            {
                service.Conectar(IpServidor, Compu);
            }
            else
            {
                service.Reconectar();
            }
        }

        private void Service_computadoraCargada(Computadora obj)
        {
            hiloUI.BeginInvoke(() =>
            {
                Compu = obj;
                IpServidor = obj.IpServidor;

            });
        }

        private void Service_ServidorApagado()
        {
            VistaActual = "ServidorApagado";
            PropertyChanged?.Invoke(this, new(nameof(VistaActual)));
        }

        private void Service_Aprobado()
        {
            VistaActual = "ServidorEncendido";
            PropertyChanged?.Invoke(this, new(nameof(VistaActual)));    
        }

        private void Conectar()
        {
            service.Conectar(IpServidor,Compu);
        }

        //public ICommand RegistrarCommand { get; set; }
       

        //public string Error { get; set; }

         

        //UdpClient Cliente = new();
        //int port = 10200;

        //public event PropertyChangedEventHandler? PropertyChanged;

        //public ClienteViewmodel()
        //{
        //    RegistrarCommand = new RelayCommand(Conectar);


        //}

        //private void RecibirMensajes()
        //{
        //    while (true)
        //    {
        //        IPEndPoint clientEP = new(IPAddress.None, 0);


        //        byte[] buffer = Cliente.Receive(ref clientEP);
        //        string comando = Encoding.UTF8.GetString(buffer);

        //        string[] comandoSeparado = comando.Split('|');

        //        if (comandoSeparado[0] == "RECHAZAR" && comandoSeparado.Length>1)
        //        {

        //           Error=comandoSeparado[1];
        //           PropertyChanged?.Invoke(this,new(nameof(Error)));


        //        }
        //    }
        //}

        //public void Conectar()
        //{
        //    if(IPAddress.TryParse(IpServidor,out IPAddress? ipServidor))
        //    Compu.Identificador = Compu.Identificador.Replace('|', '\0');
        //    IPEndPoint remoto = new IPEndPoint(ipServidor, port);
        //    var comando = $"REGISTRAR|{Compu.Identificador}";
        //    byte[] buffer = Encoding.UTF8.GetBytes(comando);



        //    Cliente.Send(buffer, buffer.Length, remoto);

        //    Thread hilo = new(RecibirMensajes);
        //    hilo.IsBackground = true;
        //    hilo.Start();
        //}
    }
}

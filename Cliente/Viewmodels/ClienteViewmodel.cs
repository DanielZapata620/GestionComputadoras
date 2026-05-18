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
        public ICommand CancelarApagadoCommand { get; set; }

        public string Error { get; set; }

        public Computadora Compu { get; set; } = new();

        public int Contador { get; set; }

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
            CancelarApagadoCommand = new RelayCommand(CancelarApagado);
            service.Aprobado += Service_Aprobado;
            service.computadoraCargada += Service_computadoraCargada;
            service.ServidorApagado += Service_ServidorApagado;
            service.EnviarError += Service_EnviarError;
            service.InvalidarIp += Service_InvalidarIp;
            service.ApagarComputadora += Service_ApagarComputadora;
            service.ActualizarTimer += Service_ActualizarTimer;

            service.InicializarCliente();

        }

        private void CancelarApagado()
        {
            service.CancelarApagado();
            
        }

        private void Service_ActualizarTimer(int tiempo)
        {
            Contador = tiempo;
            PropertyChanged?.Invoke(this, new(nameof(Contador)));
        }

        private void Service_ApagarComputadora()
        {
            Contador = 10;
            VistaActual = "Apagar";
            PropertyChanged?.Invoke(this, new(nameof(VistaActual)));
            PropertyChanged?.Invoke(this, new(nameof(Contador)));
        }

        private void Service_InvalidarIp()
        {
            Error = "Ingrese una ip valida";
            PropertyChanged?.Invoke(this, new(nameof(Error)));
            VistaActual = "Registrar";
            PropertyChanged?.Invoke(this, new(nameof(VistaActual)));
        }

        private void Service_EnviarError()
        {
            Error= "La MAC de esta computadora ya se encuentra registrada";
            PropertyChanged?.Invoke(this, new(nameof(Error)));
            VistaActual = "Registrar";
            PropertyChanged?.Invoke(this, new(nameof(VistaActual)));
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

    }
}

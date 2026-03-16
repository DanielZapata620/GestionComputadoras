using CommunityToolkit.Mvvm.Input;
using Servidor.Models;
using Servidor.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Threading;

namespace Servidor.Viewmodels
{
    public class ServidorViewmodel
    {
        public ObservableCollection<Computadora> ListaComputadoras { get; set; } = new();

        ServidorService servidorService = new ServidorService();

        public ICommand VerificarInternetCommand {get; set; }
        public ICommand ApagarCommand {get; set; }

        Dispatcher hiloUI;
        public ServidorViewmodel()
        {
            
            hiloUI = Dispatcher.CurrentDispatcher;
            servidorService.ComputadoraRegistrada += ServidorService_ComputadoraRegistrada;//
            servidorService.VerificarConexion += ServidorService_VerificarConexion;//
            VerificarInternetCommand = new RelayCommand<string>(VerificarInternet);
            servidorService.ActualizarListaComputadoras += ServidorService_ActualizarListaComputadoras;
            ApagarCommand = new RelayCommand<string>(ApagarComputadora);

            servidorService.IniciarServidor();
        }

        private void ServidorService_ActualizarListaComputadoras()
        {
            hiloUI.BeginInvoke(() =>
            {
                ListaComputadoras.Clear();
                servidorService.ListaComputadoras.ForEach(x => ListaComputadoras.Add(x));
            });
        }

        private void ApagarComputadora(string? identificador)
        {
            servidorService.ApagarComputadora(identificador);
        }

        private void ServidorService_VerificarConexion(List<Computadora> list)
        {
            hiloUI.BeginInvoke(() =>
            {
               ListaComputadoras.Clear();
               list.ForEach(x=>ListaComputadoras.Add(x));
            });
        }

        private void VerificarInternet(string identificador)
        {
            servidorService.VerificarInternet(identificador);
        }

        //private void ServidorService_VerificarConexion(Computadora computadora)
        //{

        //    hiloUI.BeginInvoke(() =>
        //    {
        //        var compuEncontrada=ListaComputadoras.FirstOrDefault(x => x.Identificador == computadora.Identificador);
        //        if (compuEncontrada != null)
        //        {
        //            compuEncontrada.Conexion = computadora.Conexion;
        //        }
        //    });
            
        //}

        private void ServidorService_ComputadoraRegistrada(Computadora computadora)
        {
            hiloUI.BeginInvoke(() =>
            {
                ListaComputadoras.Add(computadora);
            });
        }

        //public UdpClient Servidor { get; set; }

        //int puerto = 10200;

        //public event PropertyChangedEventHandler? PropertyChanged;

        //public ServidorViewmodel()
        //{
        //    IPEndPoint serverEP = new(IPAddress.Any, puerto);
        //    Servidor = new UdpClient(serverEP);

        //    Thread hilo = new(RecibirMensajes);
        //    hilo.IsBackground=true;
        //    hilo.Start();

        //}




        //public void RecibirMensajes()
        //{
        //    while(true){
        //        IPEndPoint clientEP = new(IPAddress.None, 0);


        //        byte[] buffer = Servidor.Receive(ref clientEP);
        //        string comando = Encoding.UTF8.GetString(buffer);

        //        string[] comandoSeparado = comando.Split('|');

        //        if (comandoSeparado[0] == "REGISTRAR" && comandoSeparado.Length > 1)
        //        {
        //            if (ListaComputadoras.Any(x => x.Identificador == comandoSeparado[1]))
        //            {
        //                EnviarMensaje("RECHAZAR", "Eliga otro identifiacdor , ya que el que intenta usar ya se encuentra registrado", clientEP.Address, clientEP.Port);
        //            }
        //            else
        //            {
        //                Computadora compu = new()
        //                {
        //                    Identificador = comandoSeparado[1],
        //                    IP = clientEP.Address,
        //                    Puerto = clientEP.Port,
        //                    Encendida = true
        //                };


        //                ListaComputadoras.Add(compu);

        //            }



        //        }           
        //    }
        //}

        //public void EnviarMensaje(string commando, string parametro,IPAddress ip,int port)
        //{
        //    if (commando == "RECHAZAR")
        //    {


        //        IPEndPoint remoto = new IPEndPoint(ip, port);
        //        commando += "|"+parametro;
        //        byte[] buffer = Encoding.UTF8.GetBytes(commando);


        //        Servidor.Send(buffer, buffer.Length, remoto);

        //    }

        //}
    }
}

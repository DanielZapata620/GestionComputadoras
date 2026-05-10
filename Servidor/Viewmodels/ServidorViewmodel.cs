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
using System.Windows.Media;
using System.Windows.Threading;

namespace Servidor.Viewmodels
{
    public class ServidorViewmodel: INotifyPropertyChanged
    {
        public ObservableCollection<Computadora> ListaComputadoras { get; set; } = new();

        public ObservableCollection<string> ListaLaboratorios { get; set; } = new();

        ServidorService servidorService = new ServidorService();

        private string labSeleccionado;
        public string LabSeleccionado
        {
            get => labSeleccionado;
            set
            {
                labSeleccionado = value;
                PropertyChanged?.Invoke(this, new(nameof(LabSeleccionado)));

                //servidorService.filtrarComputadorasPorLaboratorio(LabSeleccionado);
            }
        }

        public ICommand VerificarInternetCommand {get; set; }
        public ICommand EliminarCommand {get; set; }
        public ICommand FiltrarCommand {get; set; }
        public ICommand RefrescarCommand {get; set; }
        public ICommand CambiarVistaCommand {get; set; }

        public string VistaActual { get; set; } 
        public ICommand ApagarCommand {get; set; }

        Dispatcher hiloUI;

        public event PropertyChangedEventHandler? PropertyChanged;

        public ServidorViewmodel()
        {
            ListaLaboratorios.Clear();
           
            hiloUI = Dispatcher.CurrentDispatcher;
            servidorService.ComputadoraRegistrada += ServidorService_ComputadoraRegistrada;//
            servidorService.VerificarConexion += ServidorService_VerificarConexion;//

            VerificarInternetCommand = new RelayCommand<string>(VerificarInternet);
            EliminarCommand = new RelayCommand<string>(Eliminar);
            FiltrarCommand = new RelayCommand<string>(Filtrar);
            RefrescarCommand = new RelayCommand(Refrecsar);
            CambiarVistaCommand = new RelayCommand<string>(CambiarVista);

            servidorService.ActualizarListaComputadoras += ServidorService_ActualizarListaComputadoras;
            servidorService.ActualizarListaLaboratorios += ServidorService_ActualizarListaLaboratorios;
            ApagarCommand = new RelayCommand<string>(ApagarComputadora);

            servidorService.IniciarServidor();
            servidorService.ObtenerLaboratorios();
            LabSeleccionado = ListaLaboratorios.FirstOrDefault();

        }

        private void Eliminar(string? identificador)
        {
            servidorService.EliminarComputadora(identificador);
        }

        private void Refrecsar()
        {

            servidorService.VerificarStatusGlobal(false);
        }

        private void CambiarVista(string? vista)
        {
            VistaActual = vista;
            PropertyChanged?.Invoke(this, new(nameof(VistaActual)));

            if (vista == "Panel")
            {
                servidorService.filtrarComputadorasPorLaboratorio(LabSeleccionado);
            }
            else
            {
                servidorService.MostrarHistrial();
            }
                
        }

        private void Filtrar(string? lab)
        {
            LabSeleccionado = lab;
            VistaActual = "Panel";
            PropertyChanged?.Invoke(this, new(nameof(VistaActual)));
            servidorService.filtrarComputadorasPorLaboratorio(LabSeleccionado);
        }

        private void ServidorService_ActualizarListaLaboratorios()
        {
            hiloUI.BeginInvoke(() =>
            {
                ListaLaboratorios.Clear();

                servidorService.ListaLaboratorios
                    .ForEach(x => ListaLaboratorios.Add(x));

                if (VistaActual == "Historial")
                {
                    servidorService.MostrarHistrial();
                }
                else
                {
                    servidorService.filtrarComputadorasPorLaboratorio(LabSeleccionado);
                }
            });
        }

        private void ServidorService_ActualizarListaComputadoras()
        {
            if (VistaActual == "Historial")
            {
                servidorService.MostrarHistrial();
            }
            else
            {
                servidorService.filtrarComputadorasPorLaboratorio(LabSeleccionado);
            }
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



        private void ServidorService_ComputadoraRegistrada(string? lab)
        {
            LabSeleccionado = lab;
            servidorService.ObtenerLaboratorios();
        }

     
    }
}

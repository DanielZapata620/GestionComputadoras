using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace Servidor.Models
{
    public class Computadora
    {

        public string Identificador { get; set; } = null!;

        public IPAddress IP { get; set; } = null!;
        public int Puerto { get; set; }
        public bool Encendida { get; set; }
        public bool Conexion { get; set; } 
        
    }
}

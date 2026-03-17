using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace Servidor.Models
{
    public class Computadora
    {
        public string NumLaboratorio { get; set; } = null!;    
        public string  NumPc { get; set; } = null!;
        public string Identificador => $"LAB{NumLaboratorio:00}-PC{NumPc:00}";

        public string IP { get; set; } = null!;
        public int Puerto { get; set; }

        public bool Encendida { get; set; }
        public bool Conexion { get; set; } 
        
    }
}

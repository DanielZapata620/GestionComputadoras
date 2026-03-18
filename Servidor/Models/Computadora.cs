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
        public string Identificador => NumLaboratorio+"-"+NumPc;

        public bool Histroial { get; set; } ///Para el hisotrial
        public string IP { get; set; } = null!;
        public int Puerto { get; set; }

        public DateOnly FechaRegistro { get; set; }
        public DateOnly UltimaVez { get; set; }
        public bool Encendida { get; set; }
        public bool Conexion { get; set; } 
        
    }
}

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
        public string Nombre => NumLaboratorio+"-"+NumPc;
        public string Identificador { get; set; } = null!; //MAC

        public bool Histroial { get; set; } 
        public string IP { get; set; } = null!;
        public int Puerto { get; set; }

        public DateOnly FechaRegistro { get; set; }
        public DateTime UltimaVez { get; set; }
        public bool Encendida { get; set; }
        public bool Conexion { get; set; } 
        
    }
}

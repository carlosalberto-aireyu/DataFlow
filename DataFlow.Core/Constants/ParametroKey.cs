using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataFlow.Core.Constants
{
    public enum ParametroKey
    {
        [Display(Name = "Directorio de Trabajo", Description = "Ruta base para guardar los archivos generados por la aplicación.")]
        WorkDirectory,
        [Display(Name = "Directorio de Expotacion", Description = "Ruta base de exportacion de información.")]
        DataToJsonExporter
    }
}

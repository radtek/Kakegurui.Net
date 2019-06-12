using System.ComponentModel.DataAnnotations;
using System.Net;

namespace Kakegurui.Web.Models.Device
{
    /// <summary>
    /// ip格式验证
    /// </summary>
    public class IPAddressAttribute : ValidationAttribute
    {
        protected override ValidationResult IsValid(
            object value, ValidationContext validationContext)
        {
            if (value == null)
            {
                return ValidationResult.Success;
            }
            return IPAddress.TryParse(value.ToString(), out IPAddress ip) ? ValidationResult.Success : new ValidationResult("ip format verification failed");
        }

    }
}

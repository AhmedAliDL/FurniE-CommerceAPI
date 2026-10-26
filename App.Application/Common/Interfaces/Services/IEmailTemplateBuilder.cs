using App.Application.Common.Models;

namespace App.Application.Common.Interfaces.Services
{
    public interface IEmailTemplateBuilder
    {
        string Build(EmailContent content);
    }
}

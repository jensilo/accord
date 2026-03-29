using Accord.Web.Domain.Entities;
using Accord.Web.Domain.Parser;

namespace Accord.Web.Services;

public interface ITemplateService
{
    Task<List<TemplateSet>> GetTemplateSets();
    Task<List<Template>> GetTemplates(Guid templateSetId);
    Task<Template?> GetTemplate(Guid id);
    TemplateConfig ParseConfig(Template template);
}

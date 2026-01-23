using FastEndpoints;

namespace PureTCOWebApp.Features.AdminModule;

public class AdminEndpointGroup : Group
{
    public AdminEndpointGroup()
    {
        Configure("admins", ep =>
        {
            ep.Roles("Admin");
            ep.Description(x => x.WithTags("Admin Module"));
            ep.Tags("Admin Module");
        });
    }
}

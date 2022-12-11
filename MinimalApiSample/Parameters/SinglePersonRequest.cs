using MinimalApiSample.DataAccessLayer;

namespace MinimalApiSample.Parameters;

public class SinglePersonRequest
{
    public Guid Id { get; set; }

    public DataContext DataContext { get; set; }
}

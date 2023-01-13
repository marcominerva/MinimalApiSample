using MinimalApiSample.DataAccessLayer;

namespace MinimalApiSample.Requests;

public class SinglePersonRequest
{
    public Guid Id { get; set; }

    public DataContext DataContext { get; set; }
}

namespace Api.Services;

public class BarState
{
    public int CurrentGuests { get; private set; }
    public int MaxGuests { get; set; }

    public int ActiveStaff { get; set; }
    public int MaxStaff { get; set; }

    public void UpdateGuests(int delta)
    {
        CurrentGuests = Math.Max(0, CurrentGuests + delta);
    }
}
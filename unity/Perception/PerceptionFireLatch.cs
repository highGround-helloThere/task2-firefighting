public sealed class PerceptionFireLatch
{
    private bool active;

    public bool TryActivate(bool detected)
    {
        if (!detected)
        {
            active = false;
            return false;
        }

        if (active)
            return false;

        active = true;
        return true;
    }
}

public class Boxed<T> where T : struct
{
    public T value;
    public Boxed(T val)
    {
        value = val;
    }
}

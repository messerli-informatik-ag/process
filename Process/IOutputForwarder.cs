namespace Messerli.Process
{
    public interface IOutputForwarder
    {
        void WriteOutputLine(string line);

        void WriteErrorLine(string line);
    }
}

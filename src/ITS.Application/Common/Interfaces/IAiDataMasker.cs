namespace ITS.Application.Common.Interfaces;

public interface IAiDataMasker
{
    bool IsEnabled { get; }
    string Mask(string input);
}

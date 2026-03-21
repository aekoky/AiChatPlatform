namespace DocumentIngestion.Application.Exceptions;

public class DocumentParsingFailedException(string message, Exception? inner = null)
    : Exception(message, inner);

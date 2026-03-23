namespace DocumentIngestion.Application.Exceptions;

public class InvalidDocumentFormatException(string message, Exception? inner = null)
    : Exception(message, inner);

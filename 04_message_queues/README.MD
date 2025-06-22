{
  "ServiceBus": {
    "ConnectionString": "Endpoint=sb://epamtrainingsb.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=MqjNZgMNS1PTmCMmDJ5MRWLuEskFHuHGn+ASbLI0w1o=",
    "DocumentChunkQueue": "document-chunks",
    "ProcessingResultsQueue": "processing-results"
  },
  "DataCapture": {
    "InputPath": "../TestData/Input",
    "ServiceId": "DataCapture-001",
    "SupportedExtensions": [ ".pdf", ".mp4", ".zip", ".docx" ],
    "ChunkSizeKB": 100
  },
  "Processing": {
    "OutputPath": "../TestData/Output",
    "TempPath": "../TestData/Temp"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information"
    }
  }
}

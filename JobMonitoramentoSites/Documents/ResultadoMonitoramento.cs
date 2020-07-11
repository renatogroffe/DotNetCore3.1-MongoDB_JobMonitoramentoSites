using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace JobMonitoramentoSites.Documents
{
    public class ResultadoMonitoramento
    {
        public ObjectId _id { get; set; }
        public string Horario { get; set; }
        public string LocalExecucao { get; set; }
        public string Site { get; set; }
        public string Status { get; set; }
        [BsonIgnoreIfNull]
        public string DescricaoErro { get; set; }        
    }
}
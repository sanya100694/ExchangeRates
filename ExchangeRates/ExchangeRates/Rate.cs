using System.Runtime.Serialization;

namespace ExchangeRates
{
    [DataContract]
    class Rate
    {
        [DataMember]
        public int Cur_ID { get; set; }
        [DataMember]
        public string Date { get; set; }
        [DataMember]
        public string Cur_Abbreviation { get; set; }
        [DataMember]
        public int Cur_Scale { get; set; }
        [DataMember]
        public string Cur_Name { get; set; }
        [DataMember]
        public decimal? Cur_OfficialRate { get; set; }
    }
}

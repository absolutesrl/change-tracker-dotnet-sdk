using System;
using System.Collections.Generic;
using ChangeTracker.Client;

namespace ChangeTracker.Test
{
   public class TestModelSemplice
    {
        public string Id { get; set; }
        public string Descrizione { get; set; }
    }


    public class TestModel
    {
        public string Id { get; set; }
        public string Descrizione { get; set; }
        public decimal Prezzo { get; set; }
        public string Testo { get; set; }
        public DateTime Data { get; set; }
        public bool? FlagBit { get; set; }
        public List<TestModelItem> Righe { get; set; }
    }

    public class TestModelConAssociazioni : TestModel
    {
        public TestModel Utente { get; set; }
        public TestModel Anagrafica { get; set; }
    }

    public class TestModelConAssociazioniFormati : TestModel
    {
        [ModelTracker(Format = "0.0")]
        public double PrezzoTotale { get; set; }

        public float PrezzoImponibile { get; set; }

        public TestModel Utente { get; set; }
        public TestModel Anagrafica { get; set; }
    }

    public class TestModelConAssociazioniEAttributi : TestModel
    {
        [ModelTracker(Name = "UtenteData", Mapping = "Utente?.Data")]
        public string CampoMappatoComeAltro { get; set; }
        
        [ModelTracker(Name = "UtentePrezzo", Mapping = "Utente?.Prezzo")]
        public string CampoMappatoComeAltro2 { get; set; }
        
        [ModelTracker(Name = "User", Mapping = "Utente?.Descrizione")]
        public TestModel Utente { get; set; }
        
        [ModelTracker(Mapping = "Anagrafica.Descrizione")]
        public TestModel Anagrafica { get; set; }
        
        [ModelTracker(Name = "AnagraficaProdotto", Mapping = "Prodotto.Anagrafica.Descrizione")]
        public TestModelConAssociazioniEAttributi Prodotto { get; set; }

        [ModelTracker(Mapping = "ProdottoNull?.Anagrafica?.Descrizione")]
        public TestModelConAssociazioniEAttributi ProdottoNull { get; set; }

        public TestModel CampoIgnoratoNonPrimitivo { get; set; }

        [ModelTracker(Ignore = true)]
        public TestModel CampoIgnorato1 { get; set; }
        
        [ModelTracker(Ignore = true)]
        public string CampoIgnorato2 { get; set; }
    }

    public class TestModelConTypoMappingAttributi
    {
        [ModelTracker(Mapping = "Campo.Descr")]
        public TestModel Campo { get; set; }
    }

    public class TestModelNullPropMancanteTrackingAttributi
    {
        //manca volutamente un test su null
        [ModelTracker(Mapping = "Campo.Anagrafica.Descrizione")]
        public TestModelConAssociazioni Campo { get; set; }
    }

    public class TestModelItem
    {
        public string IDProdotto { get; set; }
        public string Prodotto { get; set; }

        public decimal Qta { get; set; }
        public decimal Importo { get; set; }
    }
}

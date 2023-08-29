using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using ChangeTracker.Client.Models;
using ChangeTracker.Client.Core;

namespace ChangeTracker.Test
{
    [TestClass]
    public class UnitTestFieldMapper
    {
        [TestMethod]
        public void TestDiffCreateDeleteUpdate()
        {
            var track = new Func<TestModel, Row>(m =>
            {
                return new Row
                {
                    Key = m.Id,
                    Fields = new List<Field>
                    {
                        FieldMapper.Map(m, el => el.Id),
                        FieldMapper.Map(m, el => el.Descrizione),
                        FieldMapper.Map(m, el => el.Testo),
                        FieldMapper.Map(m, el => el.Data),
                        FieldMapper.Map(m, el => el.Prezzo),
                        FieldMapper.Map(m, el => el.FlagBit)
                    }
                };
            });

            // Modello iniziale, solitamente letto da DB
            var model = new TestModel
            {
                Id = "PRIMA",
                Descrizione = "Descrizione Prima",
            };

            //a differenza del ModelTracker mi aspetto che vengano passati dei campi

            //STEP 1 - test diff su creazione
            var changeTracker = new StandardChangeCalculator();
            var rmDopo = track(model);

            var diff = changeTracker.Diff(null, rmDopo);

            diff.Desc = "Descrizione riga";
            var table = Table.CreateTable(new List<Row> { diff }, "Anagrafica", "Riccardo", "127.1.1.1");

            Assert.IsTrue(table.Rows.Count == 1);
            Assert.IsTrue(table.Rows[0].Fields.Count == 2 && table.Rows[0].Fields.All(el =>
                el.Name == "Id" || el.Name == "Descrizione"));

            var rmPrima = track(model);
            
            diff = changeTracker.Diff(rmPrima, null);
            diff.Desc = "Descrizione riga";
            table = Table.CreateTable(new List<Row> { diff }, "Anagrafica", "Riccardo", "127.1.1.1");

            Assert.IsTrue(table.Rows.Count == 1);
            Assert.IsTrue(table.Rows[0].Fields.Count == 2 && table.Rows[0].Fields.All(el =>
                el.Name == "Id" || el.Name == "Descrizione"));
        }
        
        [TestMethod]
        public void TestDiffUpdate()
        {
            var track = new Func<TestModel, Row>(m =>
            {
                return new Row
                {
                    Key = m.Id,
                    Fields = new List<Field>
                    {
                        FieldMapper.Map(m, el => el.Id),
                        FieldMapper.Map(m, el => el.Descrizione),
                        FieldMapper.Map(m, el => el.Testo),
                        FieldMapper.Map(m, el => el.Data),
                        FieldMapper.Map(m, el => el.Prezzo),
                        FieldMapper.Map(m, el => el.FlagBit)
                    }
                };
            });

            // Modello iniziale, solitamente letto da DB
            var model = new TestModel
            {
                Id = "PRIMA",
                Descrizione = "Descrizione Prima",
            };
            
            // Crea la prima traccia e informa il changeTracket che questa è la verione "precedente"
            var rmPrima = track(model);

            // Apportiamo le modifiche al modello
            model.Descrizione = "Descrizione Modificata";

            // Crea la seconda traccia e informa il changeTracket che questa è la verione "successiva"
            var rmDopo = track(model);

            // Genera le differenze indicando che siamo sulla tabella "Anagrafica" e che l'utente è Riccardo
            var diff = new StandardChangeCalculator().Diff(rmPrima, rmDopo);
            var table = Table.CreateTable(new List<Row> { diff }, "Anagrafica", "Riccardo", "127.1.1.1");

            Assert.IsTrue(table.Rows.Count == 1);
            Assert.IsTrue(table.Rows[0].Fields.Count == 1 && table.Rows[0].Fields.Any(el => el.Name == "Descrizione"));
        }
        
    }
}

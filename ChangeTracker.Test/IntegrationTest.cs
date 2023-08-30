using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using ChangeTracker.SDK;
using ChangeTracker.SDK.Core;
using ChangeTracker.SDK.Models;

namespace ChangeTracker.Test
{
    [TestClass]
    public class IntegrationTest
    {
        [TestMethod]
        [TestCategory("IgnoreOnPublish")]
        public void AtomicComponents()
        {
            var accountName = "accountName";
            var apiSecretGet = "XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX";
            var apiSecretPost = "XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX";

            var changeTrackerCalculator = new StandardChangeCalculator();

            // Funzione che fotograferà il modello costruendo la sua traccia
            var track = new Func<TestModelSemplice, Row>(model =>
            {
                return ModelTracker.Map(model, el => el.Id).Map(el => el.Descrizione).ToRow(model.Id);
            });

            // Modello iniziale, solitamente letto da DB
            var model = new TestModelSemplice()
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
            var diff = changeTrackerCalculator.Diff(rmPrima, rmDopo);

            diff.Desc = "Descrizione riga";
            var table = Table.CreateTable(new List<Row>{diff},"Anagrafica", "Riccardo", "127.1.1.1");

            // Costruisce il Token di autenticazione
            var token = TokenGenerator.GenerateToken(apiSecretPost, "Anagrafica", duration: 5);

            // Dichiara il client del microservice ChangeTracker
            var ts = new RemoteChangeStorage($"https://{accountName}.hosts.changetracker.it");

            // Salva le differenze
            var insertRes = ts.StoreChangesAsync(token, table).ConfigureAwait(false).GetAwaiter().GetResult();
            
            Assert.IsTrue(insertRes != null && insertRes.Ok, insertRes?.ErrorText);

            token = TokenGenerator.GenerateToken(apiSecretGet,"Anagrafica", "", 5);

            var getRes = ts.GetChangesAsync(token, "Anagrafica", rmDopo.Key).ConfigureAwait(false).GetAwaiter().GetResult();

            Assert.IsTrue(getRes.Ok && getRes.Changes != null && getRes.Changes.Any(), getRes?.ErrorText);
        }

        [TestMethod]
        [TestCategory("IgnoreOnPublish")]
        public void ChangeTrackerService()
        {
            var accountName = "accountName";
            var apiSecretGet = "XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX";
            var apiSecretPost = "XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX";

            var changeTracker = new ChangeTrackerService(accountName, apiSecretGet, apiSecretPost);

            // Funzione che fotograferà il modello costruendo la sua traccia
            var track = new Func<TestModelSemplice, Row>(model =>
            {
                return ModelTracker.Map(model, el => el.Id).Map(el => el.Descrizione).ToRow(model.Id);
            });

            // Modello iniziale, solitamente letto da DB
            var model = new TestModelSemplice()
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

            var response = changeTracker.Store("Anagrafica", "Riccardo", "Descrizione riga", rmPrima, rmDopo, "127.0.0.1");
            
            Assert.IsTrue(response.Ok, response?.ErrorText);
        }

        [TestMethod]
        [TestCategory("IgnoreOnPublish")]
        public void AsyncChangeTrackerService()
        {
            var accountName = "accountName";
            var apiSecretGet = "XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX";
            var apiSecretPost = "XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX";

            var changeTracker = new AsyncChangeTrackerService(accountName, apiSecretGet, apiSecretPost);

            // Funzione che fotograferà il modello costruendo la sua traccia
            var track = new Func<TestModelSemplice, Row>(model =>
            {
                return ModelTracker.Map(model, el => el.Id).Map(el => el.Descrizione).ToRow(model.Id);
            });

            // Modello iniziale, solitamente letto da DB
            var model = new TestModelSemplice()
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

            changeTracker.Store("Anagrafica", "Riccardo", "Descrizione riga", rmPrima, rmDopo, "127.0.0.1");

            Thread.Sleep(10000);
        }

        public Row Track(TestModel model)
        {
            return ModelTracker.MapAll(model).Ignore(el => el.Prezzo).ToRow(model.Id,
                new List<Table>
                {
                    model.Righe.Select(el =>
                        ModelTracker.MapAll(el).ToRow(el.IDProdotto)).ToTable("Righe")
                });
        }
    }
}

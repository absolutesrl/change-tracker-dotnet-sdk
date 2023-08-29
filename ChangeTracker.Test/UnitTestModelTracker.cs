using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using ChangeTracker.Client;
using ChangeTracker.Client.Core;
using ChangeTracker.Client.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ChangeTracker.Test
{
    [TestClass]
    public class UnitTestModelTracker
    {
        [TestMethod]
        public void TestTrackingBase()
        {
            // Questi sono i servizi di base che saranno oggeti di DependecyInjection
            var model = new TestModelConAssociazioni
            {
                Id = "PRIMA",
                Descrizione = "Descrizione Prima",
                Data = DateTime.UtcNow,
                Prezzo = 126.72M,
                FlagBit = true,
                Testo = "testo",
                Utente = new TestModel {Descrizione = "Utente associato"},
                Anagrafica = new TestModel {Descrizione = "Anagrafica associata"},
                Righe = new List<TestModelItem>
                {
                    new TestModelItem
                    {
                        IDProdotto = "P1",
                        Prodotto = "prodotto1",
                        Qta = 10,
                        Importo = 100.34M,
                    },
                    new TestModelItem
                    {
                        IDProdotto = "P2",
                        Prodotto = "prodotto2",
                        Qta = 10,
                        Importo = 100.34M,
                    }
                }
            };

            var map = ModelTracker.CreateMap(model).MapAll().Ignore(el => el.Testo)
                .Map(el => el.Descrizione + " test", "Descrizione");

            var fields = map.ToList();

            //testo non dev'essere presente
            Assert.IsTrue(fields.All(el => el.Name != "Testo"));

            //i campi presenti devono essere Id, Descrizione, Data, FlagBit. Il campo Testo rimosso, i campi associazione (Utente e Anagrafica) e la lista Righe non devono essere presenti
            Assert.IsTrue(fields.Count == 5);

            map = map.Map(el => el.Utente.Descrizione, "Utente");

            fields = map.ToList();

            //dev'essere presente anche il campo utente appena mappato
            Assert.IsTrue(fields.Any(el => el.Name == "Utente"));
        }
        
        [TestMethod]
        public void TestRowTableModel()
        {
            // Questi sono i servizi di base che saranno oggeti di DependecyInjection
            var model = new TestModelConAssociazioni
            {
                Id = "PRIMA",
                Descrizione = "Descrizione Prima",
                Data = DateTime.UtcNow,
                Prezzo = 126.72M,
                FlagBit = true,
                Testo = "testo",
                Utente = new TestModel {Descrizione = "Utente associato"},
                Anagrafica = new TestModel {Descrizione = "Anagrafica associata"},
                Righe = new List<TestModelItem>
                {
                    new TestModelItem
                    {
                        IDProdotto = "P1",
                        Prodotto = "prodotto1",
                        Qta = 10,
                        Importo = 100.34M,
                    },
                    new TestModelItem
                    {
                        IDProdotto = "P2",
                        Prodotto = "prodotto2",
                        Qta = 10,
                        Importo = 100.34M,
                    }
                }
            };

            var rowModel = ModelTracker.CreateMap(model).MapAll().ToRowModel("PRIMA",
                new List<Table>
                {
                    model.Righe.Select(el =>
                        ModelTracker.MapAll(el).ToRowModel(el.IDProdotto)).ToTableModel("Righe")
                });


            Assert.IsTrue(rowModel.Key == "PRIMA");
            Assert.IsTrue(rowModel.Tables.Count == 1 && rowModel.Tables[0].Name == "Righe");
            
            var linkedTable = rowModel.Tables[0];

            Assert.IsTrue(linkedTable.Rows.Count == 2 &&
                          linkedTable.Rows.All(el => el.Key == "P1" || el.Key == "P2"));

            var linkedRowModel = rowModel.Tables[0].Rows[0];

            Assert.IsTrue(linkedRowModel.Fields.Count == 4);
        }


        [TestMethod]
        public void TestTrackingConAttributi()
        {
            // Questi sono i servizi di base che saranno oggeti di DependecyInjection
            var model = new TestModelConAssociazioniEAttributi
            {
                Id = "PRIMA",
                Descrizione = "Descrizione Prima",
                Data = DateTime.UtcNow,
                FlagBit = true,
                Testo = "testo",
                Utente = new TestModel
                    {Descrizione = "Utente associato", Data = new DateTime(2022, 01, 10), Prezzo = 126.72M},
                Anagrafica = new TestModel { Descrizione = "Anagrafica associata" },
                Prodotto = new TestModelConAssociazioniEAttributi
                    {Anagrafica = new TestModel {Descrizione = "Anagrafica associata al Prodotto"}},
                ProdottoNull = new TestModelConAssociazioniEAttributi {Anagrafica = null},
                CampoIgnorato1 = new TestModel{Descrizione = "Ignorato"},
                CampoIgnorato2 = "Ignorato",
                CampoMappatoComeAltro = "Questo campo viene mappato come Utente?.Data",
                CampoMappatoComeAltro2 = "Questo campo viene mappato come Utente?.Prezzo",
                Righe = new List<TestModelItem>
                {
                    new TestModelItem
                    {
                        IDProdotto = "P1",
                        Prodotto = "prodotto1",
                        Qta = 10,
                        Importo = 100.34M,
                    },
                    new TestModelItem
                    {
                        IDProdotto = "P2",
                        Prodotto = "prodotto2",
                        Qta = 10,
                        Importo = 100.34M,
                    }
                }
            };

            var map = ModelTracker.MapAll(model);

            var fields = map.ToList();

            //il campo Utente dev'essere mappato come "User"
            Assert.IsTrue(fields.Any(el => el.Name == "User"));
            Assert.IsFalse(fields.Any(el => el.Name == "Utente"));

            //il campo CampoMappatoComeAltro viene mappato come Utente?.Data con nome "UtenteData" e quindi dev'essere presente un campo UtenteData di contenente una data
            Assert.IsFalse(fields.Any(el => el.Name == "CampoMappatoComeAltro"));
            Assert.IsTrue(fields.Any(el =>
                el.Name == "UtenteData" && el.PrevValue == (new DateTime(2022, 01, 10)).ToString("o")));
            
            //il campo CampoMappatoComeAltro2 viene mappato come Utente?.Prezzo con nome "UtentePrezzo" e quindi dev'essere presente un campo UtentePrezzo di contenente una prezzo con formattazione invariant
            Assert.IsFalse(fields.Any(el => el.Name == "CampoMappatoComeAltro2"));
            Assert.IsTrue(fields.Any(el => el.Name == "UtentePrezzo" && el.PrevValue == "126.72"));

            //il campo anagrafica dev'essere mappato come Anagrafica (recuperato dal nome del campo)
            Assert.IsTrue(fields.Any(el => el.Name == "Anagrafica"));

            //i campi "CampoIgnorato1" e "CampoIgnorato2" non devono essere presenti
            Assert.IsTrue(fields.All(el => el.Name != "CampoIgnorato1" && el.Name != "CampoIgnorato2"));

            //il valore del campo Prodotto deve corrispondere a model.Prodotto.Anagrafica.Descrizione come definito nell'attributo Mapping
            Assert.IsTrue(fields.Any(el => el.Name == "AnagraficaProdotto"));
            Assert.AreEqual(model.Prodotto.Anagrafica.Descrizione,
                fields.Where(el => el.Name == "AnagraficaProdotto").Select(el => el.PrevValue).FirstOrDefault());

            //il campo ProdottoNull non è valorizzato e deve corrispondere a model.Prodotto.Anagrafica.Descrizione come definito nell'attributo Mapping
            Assert.IsTrue(fields.Any(el => el.Name == "ProdottoNull"));
            Assert.IsTrue(fields.Where(el => el.Name == "ProdottoNull").Select(el => el.PrevValue).FirstOrDefault() == "");

            //mappando con Map anche i campi ignorati da attributo devono essere aggiunti
            map = map.Map(el => el.CampoIgnorato2);
            fields = map.ToList();

            Assert.IsTrue(fields.Any(el => el.Name == "CampoIgnorato2"));
            
            //i campi non primitivi sono ignorati di default da MapAll
            Assert.IsFalse(fields.Any(el => el.Name == "CampoIgnoratoNonPrimitivo"));

        }

        [TestMethod]
        public void TestErroreTypoTrackingAttributi()
        {
            //Mappo come "Campo.Descr" invece di "Campo.Descrizione" e mi aspetto una ArgumentException
            var model = new TestModelConTypoMappingAttributi
            {
                Campo = new TestModel { Descrizione = "descrizione campo" },
               
            };

            var fields = ModelTracker.MapAll(model).ToList();
        }

        [TestMethod]
        public void TestErroreNullPropMancanteTrackingAttributi()
        {
            var descrizioneAnagraficaCampo = "descrizione campo anagrafica";
            //Mappo come "Campo.Anagrafica.Descrizione" e imposto il modello Anagrafica a null prima del mapAll. Mi aspetto che restituisca un errore gestito
            var model = new TestModelNullPropMancanteTrackingAttributi
            {
                Campo = new TestModelConAssociazioni {Anagrafica = new TestModel {Descrizione = descrizioneAnagraficaCampo}}
            };

            //Prima provo che il mapping funzioni correttamente nel caso in cui il modello fosse riempito. Mi aspetto che riesca correttamente a recuperare il campo
            var fields = ModelTracker.MapAll(model).ToList();
            Assert.IsTrue(fields.Any(el => el.Name == "Campo" && el.PrevValue == descrizioneAnagraficaCampo));

            //svuoto il campo anagrafica e mi aspetto di ottenere il campo vuoto
            model.Campo.Anagrafica = null;

            fields = ModelTracker.CreateMap(model).MapAll().ToList();
            Assert.IsTrue(fields.Any(el => el.Name == "Campo" && el.PrevValue == string.Empty));
        }
        
        [TestMethod]
        public void TestMapSingoloMappingTestuale()
        {
            var descrizioneAnagrafica = "test map singolo";
            var dateTime = DateTime.UtcNow;
            //Mappo come "Campo.Anagrafica.Descrizione" e imposto il modello Anagrafica a null prima del mapAll. Mi aspetto che restituisca un errore gestito
            var model = new TestModelConAssociazioni
            {
                Anagrafica = new TestModel
                    {Descrizione = descrizioneAnagrafica, Data = dateTime, Prezzo = 15.894M, FlagBit = true}
            };

            //Prima provo che il mapping funzioni correttamente nel caso in cui il modello fosse riempito. Mi aspetto che riesca correttamente a recuperare il campo
            var fields = ModelTracker.Map(model, "Anagrafica?.Descrizione", "Descrizione")
                .Map("Anagrafica?.Data")
                .Map("Anagrafica?.Prezzo")
                .Map("Anagrafica?.FlagBit", "Bit").ToList();
            
            Assert.IsTrue(fields.Any(el => el.Name == "Descrizione" && el.PrevValue == descrizioneAnagrafica));
            Assert.IsTrue(fields.Any(el => el.Name == "AnagraficaData" && el.PrevValue == dateTime.ToString("o")));
            Assert.IsTrue(fields.Any(el => el.Name == "AnagraficaPrezzo" && el.PrevValue == "15.894"));
            Assert.IsTrue(fields.Any(el => el.Name == "Bit" && el.PrevValue == "true"));

            model.Anagrafica = null;

            fields = ModelTracker.CreateMap(model).Map("Anagrafica.Descrizione", "Descrizione").Map("Anagrafica?.FlagBit").ToList();

            Assert.IsTrue(fields.Any(el => el.Name == "Descrizione" && el.PrevValue == string.Empty));
            Assert.IsTrue(fields.Any(el => el.Name == "AnagraficaFlagBit"));
        }

        [TestMethod]
        public void TestDiffCreateDeleteModelTracker()
        {
            // Questi sono i servizi di base che saranno oggeti di DependecyInjection
            var model = new TestModelConAssociazioni
            {
                Id = "PRIMA",
                Descrizione = "Descrizione Prima",
                Utente = new TestModel { Descrizione = "Utente associato" },
                Anagrafica = new TestModel { Descrizione = "Anagrafica associata" },
                Righe = new List<TestModelItem>
                {
                    new TestModelItem
                    {
                        IDProdotto = "P1",
                    },
                    new TestModelItem
                    {
                        IDProdotto = "P2",
                        Prodotto = "prodotto2",
                        Qta = 10,
                        Importo = 100.34M,
                    }
                }
            };

            //STEP 1 - test log su creazione nuovo modello
            var mapNext = ModelTracker.CreateMap(model).MapAll().ToRowModel(model.Id,
                new List<Table>
                {
                    model.Righe.Select(el => ModelTracker.MapAll(el).ToRowModel(el.IDProdotto)).ToTableModel("Righe")
                });

            
            // Genera le differenze
            var diff = new StandardChangeCalculator().Diff(null, mapNext);
            diff.Desc = "Descrizione riga";
            var table = Table.CreateTable(new List<Row> { diff }, "Prima", "TestUser", "127.1.1.1");

            //mi aspetto che vengano mappati solo i campi Id e Descrizione che sono effettivamente riempiti
            //le associazioni non sono mappate e non vengono loggate
            Assert.IsTrue(table.Rows.Count == 1);
            Assert.IsTrue(table.Rows[0].Fields.Count == 2 && table.Rows[0].Fields.All(el => el.Name == "Id" || el.Name == "Descrizione"));

            var linkedTables = table.Rows[0].Tables;
            Assert.IsTrue(linkedTables.Count == 1 && linkedTables[0].Rows.Count == 2);
            Assert.IsTrue(linkedTables[0].Rows[0].Fields.All(el => el.Name == "IDProdotto"));
            Assert.IsTrue(linkedTables[0].Rows[1].Fields.Count == 4);

            //STEP 2 - test log su cancellazione modello
            var mapPrev = ModelTracker.CreateMap(model).MapAll().ToRowModel(model.Id, new List<Table>
            {
                model.Righe.Select(el => ModelTracker.MapAll(el).ToRowModel(el.IDProdotto)).ToTableModel("Righe")
            });
            
            // Genera le differenze
            diff = new StandardChangeCalculator().Diff(mapPrev, null);
            diff.Desc = "Descrizione riga";
            table = Table.CreateTable(new List<Row> { diff }, "Prima", "TestUser", "127.1.1.1");

            //mi aspetto che vengano mappati solo i campi Id e Descrizione che sono effettivamente riempiti
            //le associazioni non sono mappate e non vengono loggate
            Assert.IsTrue(table.Rows.Count == 1);
            Assert.IsTrue(table.Rows[0].Fields.Count == 2 && table.Rows[0].Fields.All(el => el.Name == "Id" || el.Name == "Descrizione"));

            linkedTables = table.Rows[0].Tables;

            Assert.IsTrue(linkedTables.Count == 1 && linkedTables[0].Rows.Count == 2);
            Assert.IsTrue(linkedTables[0].Rows[0].Fields.All(el => el.Name == "IDProdotto"));
            Assert.IsTrue(linkedTables[0].Rows[1].Fields.Count == 4);
        }

        [TestMethod]
        public void TestDiffUpdateModelTracker()
        {
            // Questi sono i servizi di base che saranno oggeti di DependecyInjection
            var model = new TestModelConAssociazioni
            {
                Id = "PRIMA",
                Descrizione = "Descrizione Prima",
                Data = DateTime.UtcNow,
                Prezzo = 126.72M,
                FlagBit = true,
                Utente = new TestModel {Descrizione = "Utente associato"},
                Anagrafica = new TestModel {Descrizione = "Anagrafica associata"},
                Righe = new List<TestModelItem>
                {
                    new TestModelItem
                    {
                        IDProdotto = "P1",
                        Prodotto = "prodotto1",
                        Qta = 10,
                        Importo = 100.34M,
                    },
                    new TestModelItem
                    {
                        IDProdotto = "P2",
                        Prodotto = "prodotto2",
                        Qta = 10,
                        Importo = 100.34M,
                    }
                }
            };

            var mapPrev = ModelTracker.CreateMap(model).MapAll().ToRowModel(model.Id);
            
            mapPrev.Tables.Add(model.Righe.Select(el => ModelTracker.MapAll(el).ToRowModel(el.IDProdotto)).ToTableModel("Righe"));

            model.Descrizione = "Descrizione Modificata";

            model.Righe[0] = new TestModelItem
            {
                IDProdotto = "P3",
                Prodotto = "prodotto3",
                Qta = 15,
                Importo = 80.34M
            };
            
            var mapNext = ModelTracker.CreateMap(model).MapAll().ToRowModel(model.Id);

            mapNext.Tables.Add(model.Righe.Select(el => ModelTracker.MapAll(el).ToRowModel(el.IDProdotto))
                .ToTableModel("Righe"));

            // Genera le differenze indicando che siamo sulla tabella "Anagrafica" e che l'utente è Riccardo
            var diff = new StandardChangeCalculator().Diff(mapPrev, mapNext);
            diff.Desc = "Descrizione riga";
            var table = Table.CreateTable(new List<Row> { diff }, "Prima", "TestUser", "127.1.1.1");

            Assert.IsTrue(table.Rows.Count == 1);
            Assert.IsTrue(table.Rows[0].Fields.Count == 1 && table.Rows[0].Fields.Any(el => el.Name == "Descrizione"));

            var linkedTables = table.Rows[0].Tables;

            Assert.IsTrue(linkedTables.Count == 1 && linkedTables[0].Rows.Count == 2);
            Assert.IsTrue(linkedTables[0].Rows[0].Fields.Count == 4 && linkedTables[0].Rows[1].Fields.Count == 4);

            //Nella tabella righe il diff dovrebbe aggiungere la riga con id P3 e rimuovere la riga con id P1
            Assert.IsTrue(linkedTables[0].Rows[0].Fields.Any(x => x.Name == "IDProdotto" && x.PrevValue == "P1"));
            Assert.IsTrue(linkedTables[0].Rows[1].Fields.Any(x => x.Name == "IDProdotto" && x.NextValue == "P3"));
        }

        [TestMethod]
        public void TestFormatModelTracker()
        {
            // Questi sono i servizi di base che saranno oggeti di DependecyInjection
            var model = new TestModelConAssociazioniFormati
            {
                Id = "PRIMA",
                Descrizione = "Descrizione Prima",
                Data = DateTime.UtcNow,
                Prezzo = 126.72M,
                FlagBit = true,
                Testo = "testo",
                Utente = new TestModel { Descrizione = "Utente associato" },
                Anagrafica = new TestModel { Descrizione = "Anagrafica associata" },
                PrezzoTotale = 5,
                PrezzoImponibile = 10,
                Righe = new List<TestModelItem>
                {
                    new TestModelItem
                    {
                        IDProdotto = "P1",
                        Prodotto = "prodotto1",
                        Qta = 10,
                        Importo = 100.34M,
                    },
                    new TestModelItem
                    {
                        IDProdotto = "P2",
                        Prodotto = "prodotto2",
                        Qta = 10,
                        Importo = 100.34M,
                    }
                }
            };

            var map = ModelTracker.CreateMap(model).MapAll().Ignore(el => el.Testo)
                .Map(el => el.Descrizione + " test", "Descrizione")
                .Map(el => el.PrezzoImponibile, format: "0.0000");

            var fields = map.ToList();

            //il prezzo totale ha come formato "0.0" e quindi dev'essere convertito a una cifra decimale
            Assert.IsTrue(fields.Any(el => el.Name == "PrezzoTotale" && el.PrevValue == "5.0"));

            Assert.IsTrue(fields.Any(el => el.Name == "PrezzoImponibile" && el.PrevValue == "10.0000"));


            //Testo le differenze con la formattazione
            var modelDopo = new TestModelConAssociazioniFormati
            {
                Id = "PRIMA",
                Descrizione = "Descrizione Prima",
                Data = DateTime.UtcNow,
                Prezzo = 126.72M,
                FlagBit = true,
                Testo = "testo",
                Utente = new TestModel { Descrizione = "Utente associato" },
                Anagrafica = new TestModel { Descrizione = "Anagrafica associata" },
                PrezzoTotale = 5.0000,
                PrezzoImponibile = 10,
                Righe = new List<TestModelItem>
                {
                    new TestModelItem
                    {
                        IDProdotto = "P1",
                        Prodotto = "prodotto1",
                        Qta = 10,
                        Importo = 100.34M,
                    },
                    new TestModelItem
                    {
                        IDProdotto = "P2",
                        Prodotto = "prodotto2",
                        Qta = 10,
                        Importo = 100.34M,
                    }
                }
            };

            var mapDopo = ModelTracker.CreateMap(modelDopo).MapAll().Ignore(el => el.Testo)
                .Map(el => el.Descrizione + " test", "Descrizione")
                .Map(el => el.PrezzoImponibile, format: "0.0000");
            
            //Nonostante prima sia PrezzoTotale = 5 e dopo 5.0000 mi aspetto di non vedere modifiche su quel campo perché c'è la formattazione
            var diff = new StandardChangeCalculator().Diff(map.ToRowModel(model.Id), mapDopo.ToRowModel(model.Id));
            diff.Desc = "Descrizione riga";
            var table = Table.CreateTable(new List<Row> { diff }, "Prima", "TestUser", "127.1.1.1");

            Assert.IsFalse(table.Rows[0].Fields.Any(el => el.Name == "PrezzoTotale"));
        }
    }
}
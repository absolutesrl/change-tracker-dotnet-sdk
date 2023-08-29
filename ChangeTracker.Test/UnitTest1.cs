using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using ChangeTracker.Client.Models;
using ChangeTracker.Client.Core;

namespace ChangeTracker.Test
{
    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        public void TestSemplice()
        {
            // Questi sono i servizi di base che saranno oggeti di DependecyInjection
            var changeTrackerCalculator = new StandardChangeCalculator();

            // Funzione che fotograferà il modello costruendo la sua traccia
            var track = new Func<TestModelSemplice, Row>(model =>
            {
                return new Row
                {
                    Key = model.Id,
                    Fields = new List<Field>
                    {
                        FieldMapper.Map(model, el => el.Id),
                        FieldMapper.Map(model, el => el.Descrizione)
                    },
                };
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
            var token = TokenGenerator.GenerateToken("XDTpMcGHS8a2eGaiUZqHXLUmshSTWXBU", "Anagrafica", duration: 5);

            // Dichiara il client del microservice ChangeTracker
            var ts = new RemoteChangeStorage("https://test.hosts.changetracker.it");

            // Salva le differenze
            var insertRes = ts.StoreChangesAsync(token, table).ConfigureAwait(false).GetAwaiter().GetResult();
            
            Assert.IsTrue(insertRes != null && insertRes.Ok, insertRes?.ErrorText);

            token = TokenGenerator.GenerateToken("qCWpA3XNEBMhXZtwgP95C8RBzanWt3","Anagrafica", "", 5);

            var getRes = ts.GetChangesAsync(token, "Anagrafica", rmDopo.Key).ConfigureAwait(false).GetAwaiter().GetResult();

            Assert.IsTrue(getRes.Ok && getRes.Changes != null && getRes.Changes.Any(), getRes?.ErrorText);
        }

        //[TestMethod]
        //public void TestInsertAWS()
        //{
        //    //var enc = "U2FsdGVkX18ZYg/GPvqGBI0OfEWIYGKkZJDG2pq2d/Q=";
        //    //var key = "UARdi72HTfRQLCxCX7mgWD22TpREngTg";
        //    //var plainText = "AAAPROVA";

        //    //var test1 = AesOperations.EncryptString(key, plainText);

        //    //var test = AesOperations.DecryptString(key, test1);

        //    var cc = new StandardChangeCalculator();
        //    var ct = new Client.ChangeTracker(cc);

        //    var prev = new TestModel
        //    {
        //        Id = "PRIMA",
        //        Descrizione = "Descrizione Prima",
        //        Testo = "testo di prova lungo",
        //        Data = DateTime.Now,
        //        Righe = new List<TestModelItem>
        //        {
        //            new TestModelItem {IDProdotto = "P1", Prodotto = "Primo prodotto modificato", Qta = 20, Importo = 100},
        //            new TestModelItem {IDProdotto = "P2", Prodotto = "Secondo prodotto", Qta = 10, Importo = 100},
        //            new TestModelItem {IDProdotto = "P4", Prodotto = "Quarto prodotto", Qta = 12, Importo = 123}
        //        }
        //    };

        //    var rmPrima = Track(prev);
        //    ct.SetPreviousVersion(rmPrima);

        //    var next = new TestModel
        //    {
        //        Id = "PRIMA",
        //        Descrizione = "Descrizione Dopo",
        //        Testo = "testo di prova lungo",
        //        Data = prev.Data.AddHours(1),
        //        FlagBit = true,
        //        Righe = new List<TestModelItem>
        //        {
        //            new TestModelItem {IDProdotto = "P1", Prodotto = "Primo prodotto", Qta = 10, Importo = 100},
        //            new TestModelItem {IDProdotto = "P3", Prodotto = "Terzo prodotto", Qta = 30, Importo = 300},
        //            new TestModelItem {IDProdotto = "P4", Prodotto = "Quarto prodotto", Qta = 12, Importo = 123}
        //        }
        //    };

        //    var rmDopo = Track(next);

        //    ct.SetNextVersion(rmDopo);

        //    var diff = ct.Diff("TABELLA1", "Pippo");

        //    var tokenService = new TokenService("dnN9oNYHFFRLPGrG", "UARdi72HTfRQLCxCX7mgWD22TpREngTg");

        //    var token = tokenService.CreateToken();

        //    var ts = new RemoteChangeStorage("https://test.changetracker.it");

        //    var store = ts.StoreChangesAsync(token, diff).Result;

        //    var list = ts.GetChangesAsync(token, "TABELLA1").Result;

        //}

        //[TestMethod]
        //public void TestGetChangesAWS()
        //{
        //    var ts = new RemoteChangeStorageOptions(
        //        @"https://125ve28oag.execute-api.eu-south-1.amazonaws.com/prod/",
        //        "dnN9oNYHFFRLPGrG"
        //    );

        //    var result = ts.GetChangesAsync("TABELLA1");

        //    Task.WaitAll(result);

        //    var items = result.Result;
        //}

        //[TestMethod]
        //public void TestMethod1()
        //{
        //    var ts = new LocalChangeStorage();
        //    var cc = new StandardChangeCalculator();
        //    var ct = new Client.ChangeTracker(ts, cc);
        //    ct.InitTracking("TABELLA1", "Pippo");

        //    var prev = new TestModel
        //    {
        //        Id = "PRIMA",
        //        Descrizione = "Descrizione Prima",
        //        Testo = "testo di prova lungo",
        //        Data = DateTime.Now,
        //        Righe = new List<TestModelItem>
        //        {
        //            new TestModelItem
        //                {IDProdotto = "P1", Prodotto = "Primo prodotto modificato", Qta = 20, Importo = 100},
        //            new TestModelItem {IDProdotto = "P2", Prodotto = "Secondo prodotto", Qta = 10, Importo = 100},
        //            new TestModelItem {IDProdotto = "P4", Prodotto = "Quarto prodotto", Qta = 12, Importo = 123}
        //        }
        //    };

        //    var rmPrima = Track(prev);
        //    ct.SetPreviousVersion(rmPrima);

        //    var next = new TestModel
        //    {
        //        Id = "PRIMA",
        //        Descrizione = "Descrizione Dopo",
        //        Testo = "testo di prova lungo",
        //        Data = prev.Data.AddHours(1),
        //        FlagBit = true,
        //        Righe = new List<TestModelItem>
        //        {
        //            new TestModelItem {IDProdotto = "P1", Prodotto = "Primo prodotto", Qta = 10, Importo = 100},
        //            new TestModelItem {IDProdotto = "P3", Prodotto = "Terzo prodotto", Qta = 30, Importo = 300},
        //            new TestModelItem {IDProdotto = "P4", Prodotto = "Quarto prodotto", Qta = 12, Importo = 123}
        //        }
        //    };

        //    var rmDopo = Track(next);

        //    ct.SetNextVersion(rmDopo);

        //    var diff = ct.LogDiff();
        //}

        public Row Track(TestModel model)
        {
            var rm = new Row
            {
                Key = model.Id,
                Fields = new List<Field>
                {
                    FieldMapper.Map(model, el => el.Id),
                    FieldMapper.Map(model, el => el.Descrizione),
                    FieldMapper.Map(model, el => el.Testo),
                    FieldMapper.Map(model, el => el.FlagBit),
                    FieldMapper.Map(model, el => el.Data)
                },
                Tables = new List<Table>
                {
                    new Table
                    {
                        Name = "Righe",
                        Rows = model.Righe.Select(el => new Row
                        {
                            Key = el.IDProdotto,
                            Fields = new List<Field>
                            {
                                FieldMapper.Map(el, el => el.Prodotto),
                                FieldMapper.Map(el, el => el.Qta),
                                FieldMapper.Map(el, el => el.Importo)
                            }
                        }).ToList()
                    }
                }
            };

            return rm;
        }
    }

    [TestClass]
    public class TestServer
    {
        private string _tableNameDynamo = "changes";

        //string _tableName = "ANAGRAFICA";
        string _tableName = "CONTRATTI";


        //[TestMethod]
        //public void TestSer()
        //{
        //    var tableModel = new Table
        //    {
        //        TableName = "TEST",
        //        Rows = new List<Row>
        //        {
        //            new Row
        //            {
        //                TableName = "TEST",
        //                OperationDateTime = DateTime.UtcNow,
        //                RowKey = "A02",
        //                State = RowStatus.Modified,
        //                UserName = "Riccardo"
        //            }
        //        }
        //    };

        //    var ser = JsonSerializer.Serialize(tableModel);

        //}

        //[TestMethod]
        //public void TestRead()
        //{
        //    var server = new ChangeTrackerServer();
        //    //server.CheckVersion();

        //    var all = new List<Row>();
        //    var ret = new GetChangesResult();
        //    do
        //    {
        //        ret = server.GetChanges(_tableName, "C72", ret.PaginationToken);
        //        all.AddRange(ret.Changes);
        //    } while (DynamoEof(ret.PaginationToken));

        //    Console.WriteLine("A");
        //}

        //private bool DynamoEof(string paginationToken)
        //{
        //    return 
        //        !string.IsNullOrEmpty(paginationToken) && paginationToken.Length > 5;
        //}

        //[TestMethod]
        //public void TestCreation()
        //{
        //    var server = new ChangeTrackerServer();
        //    server.CheckVersion();
        //    var vetStates = new string[3];
        //    vetStates[0] = RowStatus.New;
        //    vetStates[1] = RowStatus.Deleted;
        //    vetStates[2] = RowStatus.Modified;

        //    var tables = new string[2];
        //    tables[0] = "ANAGRAFICA";
        //    tables[1] = "CONTRATTI";

        //    var num = 1000;

        //    Parallel.For(0, num, (i =>
        //    {
        //        var index = new Random().Next(0, (int) num / 2);
        //        var rn = new Random().Next(0, 3);
        //        var tr = new Random().Next(0, 2);
        //        var days = new Random().Next(0, 300);
        //        var hours = new Random().Next(0, 100);
        //        var min = new Random().Next(0, 500);

        //        server.InsertChange(new Table
        //        {
        //            TableName = tables[tr],
        //            Rows = new List<Row>
        //            {
        //                new Row
        //                {
        //                    TableName = tables[tr],
        //                    State = vetStates[rn],
        //                    RowKey = "C" + index,
        //                    DateTime = DateTime.UtcNow.AddDays(days).AddHours(hours).AddMinutes(min),
        //                    Fileds = new List<Field>
        //                    {
        //                        new Field {Name = "campo1", PrevValue = "old " + index, NextValue = "new " + index}
        //                    }
        //                }
        //            }
        //        });
        //    }));


        //    Console.Write("A");
        //}

        //[TestMethod]
        //public void TestRead()
        //{
        //    var client = new AmazonDynamoDBClient(
        //        new BasicAWSCredentials("qe5qbf", "wfyyl"),
        //        new AmazonDynamoDBConfig
        //        {
        //            RegionEndpoint = null,
        //            ServiceURL = "http://localhost:8000/"
        //        });

        //    var config = new DynamoDBContextConfig
        //    {
        //        Conversion = DynamoDBEntryConversion.V2
        //    };
        //    AWSConfigsDynamoDB.Context.TypeMappings[typeof(Row)] = new Amazon.Util.TypeMapping(typeof(Row), _tableNameDynamo);
        //    var ddbContext = new DynamoDBContext(client, config);

        //    var partitionKeyValue = new Dictionary<string, DynamoDBEntry>
        //    {
        //        {":hashKey", "CONTRATTI"},
        //        {":rangeKey", "C72#"}
        //    };

        //    var table = Table.LoadTable(client, _tableNameDynamo);
        //    var search = table.Query(new QueryOperationConfig
        //    {
        //        Limit = 10,
        //        BackwardSearch = true,
        //        KeyExpression = new Expression
        //        {
        //            ExpressionStatement = "(HashKey = :hashKey) and begins_with(RangeKey, :rangeKey)",
        //            //ExpressionStatement = "(HashKey = :hashKey)",
        //            ExpressionAttributeValues = partitionKeyValue
        //        },
        //        //IndexName = "lsi"
        //        //PaginationToken = "{\"TableName\":{\"S\":\"CONTRATTI\"},\"Id\":{\"S\":\"C278#M#20210531170528968#iwrg\"}}"
        //    });

        //    var rows = new List<Row>();

        //    var nextSet = search.GetNextSetAsync().Result;
        //    rows.AddRange(nextSet.Select(el => ddbContext.FromDocument<Row>(el)));

        //    var pt = search.PaginationToken;

        //    //do
        //    //{
        //    //    var nextSet = search.GetNextSetAsync().Result;
        //    //    rows.AddRange(nextSet.Select(el => ddbContext.FromDocument<Row>(el)));
        //    //}
        //    //while (!search.IsDone);

        //    Console.WriteLine("A");
        //}

        //[TestMethod]
        //public void TestRead()
        //{
        //    var client = new AmazonDynamoDBClient(
        //        new BasicAWSCredentials("qe5qbf", "wfyyl"),
        //        new AmazonDynamoDBConfig
        //        {
        //            RegionEndpoint = null,
        //            ServiceURL = "http://localhost:8000/"
        //        });

        //    AWSConfigsDynamoDB.Context.TypeMappings[typeof(Row)] =
        //        new Amazon.Util.TypeMapping(typeof(Row), _tableNameDynamo);
        //    var config = new Amazon.DynamoDBv2.DataModel.DynamoDBContextConfig
        //    {
        //        Conversion = DynamoDBEntryConversion.V2
        //    };

        //    var ddbContext = new DynamoDBContext(client, config);

        //    var tableValue = new Dictionary<string, DynamoDBEntry>
        //    {
        //        {":v_TableId", _tableName},
        //        {":v_Id", "C278#"}
        //    };

        //    var scan = ddbContext.FromQueryAsync<Row>(
        //        new QueryOperationConfig
        //        {
        //            KeyExpression = new Expression
        //            {
        //                ExpressionStatement = "TableName = :v_TableId " +
        //                                      "AND begins_with(Id, :v_Id)",
        //                ExpressionAttributeValues = tableValue
        //            },
        //            PaginationToken = "C278#N#20210531170456279#jrql"
        //        },
        //        new DynamoDBOperationConfig
        //        {

        //        }
        //    );

        //    var results = scan.GetNextSetAsync().Result;
        //}

        //[TestMethod]
        //public void TestRead()
        //{
        //    var client = new AmazonDynamoDBClient(
        //        new BasicAWSCredentials("qe5qbf", "wfyyl"),
        //        new AmazonDynamoDBConfig
        //        {
        //            RegionEndpoint = null,
        //            ServiceURL = "http://localhost:8000/"
        //        });

        //    var moviesTable = Table.LoadTable(client, _tableNameDynamo);

        //    // start initial query
        //    var search = moviesTable.Query(new Primitive(_tableName), 
        //        new QueryFilter("Id", QueryOperator.BeginsWith, 
        //            new List<AttributeValue>(){ new AttributeValue("A10#") })
        //    );

        //    // retrieve one pages of items
        //    var items = search.GetNextSetAsync().Result;


        //    // get pagination token
        //    string token = search.PaginationToken;

        //}


    }
}

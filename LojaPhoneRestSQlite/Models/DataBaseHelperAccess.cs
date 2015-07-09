using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SQLite;
using System.IO;
using Windows.Storage;
using System.Collections.ObjectModel;
using Windows.System;
using Windows.UI.Popups;
using System.Diagnostics;



namespace LojaPhoneRestSQlite.Models
{
    class DataBaseHelperAccess
    {
        SQLiteConnection dbConn;
        private string DB_path = Path.Combine(Path.Combine(ApplicationData.Current.LocalFolder.Path, "banco.sqlite"));



        public List<Fabricante> LendoFabricantesLocal()
        {
            using(dbConn = new SQLiteConnection(DB_path))
            {
                //dbConn.CreateTable<Fabricante>();
                List<Fabricante> fabricantes = dbConn.Table<Fabricante>().ToList();
                dbConn.Close();
                return fabricantes;
            }
        }

        public void Inserindo(Fabricante f)
        {
            try
            {
                dbConn = new SQLiteConnection(DB_path);
                dbConn.CreateTable<Fabricante>();
                dbConn.Insert(f);
                //dbConn.Dispose();
                dbConn.Close();

            }
            catch(Exception e)
            {
                System.Diagnostics.Debug.WriteLine("##Erro Inserindo## "+e);
            }
        }

        public void InserindoListaFabricantes(List<Fabricante> fabricantes)
        {
            using (var dbConn = new SQLiteConnection(DB_path))
            {
                dbConn.DropTable<Fabricante>();
                dbConn.Dispose();
                dbConn.Close();
            }
            foreach (Fabricante f in fabricantes)
                this.Inserindo(f);
        }

        public void criando_populando(string s)
        {
            dbConn = new SQLiteConnection(DB_path);
            SQLiteCommand cmd = new SQLiteCommand(dbConn);
            cmd.CommandText = s;
            cmd.ExecuteNonQuery();
            dbConn.Close();
        }
    }
}

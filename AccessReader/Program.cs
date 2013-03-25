using System;
using DatabaseManagement;

namespace AccessReader
{
    class Program
    {
        static void Main(string[] args)
        {
            IDatabase db = Database.Instanse;
            try
            {
                if( !db.Connect( @"..\..\..\TestDataBase\Test.mdb" ) )
                {
                    throw new Exception( "データベースに接続できませんでした！" );
                }
                Console.WriteLine( "データベースに接続しました！" );

                string sql = "select * from 品目マスタ";
                IDatabaseRecordList list = new DatabaseRecordList( db.Read( sql ) );
                Console.WriteLine( "-".PadLeft( 25, '-' ) );
                foreach ( IDatabaseRecord r in list )
                {
                    Console.Write( "|" );
                    Console.Write( r.Name.PadRight( 8, '　' ) );
                    Console.Write( "|" );
                    Console.Write( "{0, 5}", r.Price.ToString() );
                    Console.Write( "|\n" );
                }
                Console.WriteLine( "-".PadLeft( 25, '-' ) );
            }
            catch ( Exception ex )
            {
                Console.WriteLine( ex.Message );
            }
            finally
            {
                db.Disconnect();
            }
            string s = "何かキーを押すと終了します...";
            Console.WriteLine( s );
            Console.ReadKey( true );
        }
    }
}

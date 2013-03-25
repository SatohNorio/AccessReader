using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Odbc;

namespace DatabaseManagement
{
    using RecordList  = List<IDatabaseRecord>;

    // %%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%
    // データベースを操作するインターフェース
    public interface IDatabase : IDisposable
    {
        // ====================================================================
        // 引数の接続文字列を使用してデータベースにODBC接続する
        bool Connect( string path );

        // ====================================================================
        // データベースを切断する
        void Disconnect();

        // ====================================================================
        // SQLのSELECT文を渡して問合せ結果を取得する
        OdbcDataReader Read( string sql );

        // ====================================================================
        // データベースを操作するSQLを渡す
        int Update( string sql );

        // ====================================================================
        // データベースの接続状態を保持する
        bool IsConnected { get; }
    }

    // ########################################################################
    // シングルトンパターンを使用し
    // データベースに接続できるオブジェクトは一つに制限する
    public class Database : IDatabase 
    {
        // ====================================================================
        // オブジェクトにアクセスする唯一の手段
        public static IDatabase Instanse
        {
            get { return _Database; }
        }

        // ====================================================================
        // 終了処理
        public void Dispose()
        {
            if ( this.IsConnected )
            {
                this.Disconnect();
                this.Connection = null;
            }
        }

        // ====================================================================
        // データベースに接続する
        // 引数にデータベースの存在するパスを指定する
        public bool Connect( string path )
        {
            try
            {
                if ( !this.IsConnected )
                {
                    string strConnection = string.Format(
                             "DRIVER={0:s};DBQ={1:s}", DATABASE_DRIVER, path );

                    OdbcConnection c = this.Connection;
                    c.ConnectionString = strConnection;
                    c.Open();

                    this.IsConnected = true;
                }
            }
            catch ( Exception /*ex*/ )
            {
                this.IsConnected = false;
            }
            return this.IsConnected;
        }

        // ====================================================================
        // データベースを切断する
        public void Disconnect()
        {
            try
            {
                OdbcConnection c = this.Connection;
                if ( c != null )
                {
                    c.Close();
                    this.IsConnected = false;
                }
            }
            catch ( Exception /*ex*/ )
            {
                this.IsConnected = false;
            }
        }

        // ====================================================================
        // SQLを実行してデータセットを取得する
        public OdbcDataReader Read( string sql )
        {
            OdbcCommand cmd = new OdbcCommand();
            cmd.CommandText = sql;
            cmd.CommandType = CommandType.Text;
            cmd.Connection = this.Connection;

            return cmd.ExecuteReader();
        }

        // ====================================================================
        // データベースを更新するSQLを渡す
        // 戻り値には変更したデータの件数を返す
        // データの更新が行われないSQLが渡された場合は-1を返す
        public int Update( string sql )
        {
            OdbcCommand cmd = new OdbcCommand();
            cmd.CommandText = sql;
            cmd.CommandType = CommandType.Text;
            cmd.Connection = this.Connection;

            return cmd.ExecuteNonQuery();
        }

        // データベースの接続状態を保持する
        public bool IsConnected { get; private set; }   

        // $$$$$ private $$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$
        private const string DATABASE_DRIVER =  "{Microsoft Access Driver (*.mdb)}";

        // ODBC接続オブジェクト
        private OdbcConnection Connection { get; set; }   

        // ただ一つのDatabaseManagerクラスのインスタンス
        private static  IDatabase _Database = new Database();   

        // ====================================================================
        // コンストラクタ
        // Privateなので、外部からインスタンスを作成することは不可
        private Database() 
        {
            this.Connection = new OdbcConnection();
        }

        // ====================================================================
        // コピーコンストラクタ
        // Privateにして、外部からは使用不可
        private Database( IDatabase obj ) {}
    }

    // %%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%
    // データベースのレコード一件を表すインターフェース
    public interface IDatabaseRecord
    {
        // ====================================================================
        int Key { get; }
        string Name { get; }
        int Price { get; }
    }

    // %%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%
    // データベースのレコードを格納するコンテナの動作を定義したインターフェース
    public interface IDatabaseRecordList 
    {
        // ====================================================================
        // インデクサ
        IDatabaseRecord this[int index] { get; }

        // ====================================================================
        // データ件数を返す
        int Count { get; }

        // ====================================================================
        // foreach構文で使用できるように定義する
        RecordList.Enumerator GetEnumerator();
    }

    // ########################################################################
    // １件のレコードを表現する抽象クラス
    public class DatabaseRecord : IDatabaseRecord
    {
        // ====================================================================
        // コンストラクタ
        public DatabaseRecord( OdbcDataReader reader )
        {
            this.Key = reader.GetInt32(0);
            this.Name = reader.GetString(1);
            this.Price = reader.GetInt32(2);
        }

        // ====================================================================
        public int Key
        {
            get { return this._key; }
            private set { this._key = value; }
        }

        // ====================================================================
        public string Name
        {
            get { return this._name; }
            private set { this._name = value; }
        }

        // ====================================================================
        public int Price
        {
            get { return this._price; }
            private set { this._price = value; }
        }

        // $$$$$ private $$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$
        private int _key;
        private string _name;
        private int _price;
    }

    // ########################################################################
    // レコードのコレクションクラス
    public class DatabaseRecordList : IDatabaseRecordList
    {
        // ====================================================================
        // コンストラクタ
        public DatabaseRecordList( OdbcDataReader reader )
        {
            RecordList list = this._list;

            list.Clear();
            while ( reader.Read() )
            {
                IDatabaseRecord d = new DatabaseRecord( reader );
                list.Add( d );
            }
        }

        // ====================================================================
        // インデクサ
        public IDatabaseRecord this[int index]
        {
            get{ return this._list[index]; }
        }
        
        // ====================================================================
        // データ件数を返すプロパティ
        public int Count
        {
            get{ return this._list.Count; }
        }

        // ====================================================================
        // foreach構文で使用できるように定義する
        public RecordList.Enumerator GetEnumerator()
        {
            return this._list.GetEnumerator();
        }

        // $$$$$ private $$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$
        private RecordList      _list = new RecordList();
    }
}

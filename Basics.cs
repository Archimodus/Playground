using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Text.RegularExpressions;

namespace HomeControl
{
	using uint8_t = System.Byte;
	using uint16_t = System.UInt16;
	using int8_t = System.SByte;
	using int16_t = System.Int16;
	using uint32_t = System.UInt32;
	using int32_t = System.Int32;

	public class Conversions
	{
		public static uint8_t	lowNibble( uint8_t val )
		{
			uint16_t uv = (uint16_t) val;
			return (uint8_t) ( uv & 0x0F );
		}

		public static uint8_t	highNibble( uint8_t val )
		{
			uint16_t uv = (uint16_t) val;
			return (uint8_t) ( ( uv >> 4 ) & 0x0F );
		}

		public static uint8_t	lowByte( uint16_t val )
		{
			return (uint8_t) ( val & 0xFF );
		}

		public static uint8_t 	highByte( uint16_t val )
		{
			return (uint8_t) ( ( val >> 8 ) & 0xFF );
		}

		public static uint16_t	lowWord( uint32_t val )
		{
			return (uint16_t) ( val & 0xFFFF );
		}

		public static uint16_t 	highWord( uint32_t val )
		{
			return (uint16_t) ( ( val >> 16 ) & 0xFFFF );
		}

		public static uint8_t	buildByte( uint8_t highNibble, uint8_t lowNibble )
		{ 
			return (uint8_t) ( ( highNibble << 4 ) + lowNibble );
		}

		public static uint16_t	buildWord( uint8_t highByte, uint8_t lowByte )
		{
			uint16_t	result = (uint16_t) highByte;
			result <<= 8;
			result += lowByte;
			return result;
		}

		public static uint32_t	buildDword( uint16_t highWord, uint16_t lowWord )
		{
			uint32_t	result = (uint32_t) highWord;
			result <<= 16;
			result += lowWord;
			return result;
		}

	}

	public class CRC8
	{
		protected uint8_t _crc_ibutton_update( uint8_t crc, uint8_t data )
		{
			crc = (uint8_t) ( crc ^ data );
			for( uint8_t i = 0; i < 8; i++ ) {
				if( ( crc & 0x01 ) != 0 )
					crc = (uint8_t) ( ( crc >> 1 ) ^ 0x8C );
				else
					crc >>= 1;
			}
			return crc;
		}

		protected	uint8_t current_;

		public		uint8_t		Value { get { return current_; } }

		public		CRC8( uint8_t seed )
		{
			current_ = seed;
		}

		public	uint8_t	updateByte( uint8_t byte_val )
		{
			current_ = _crc_ibutton_update( current_, byte_val );
			return current_;
		}

		public	uint8_t	updateBytes( uint8_t[] byte_arr, uint16_t startPos, uint16_t len )
		{
			uint16_t	remaining = len;
			uint16_t	pos = startPos;
			for(; remaining != 0; remaining--, pos++ ) {
				uint8_t byte_val = byte_arr[ pos ];
				updateByte( byte_val );
			}
			return Value;
		}
	
	}

	public class Encoding
	{
		protected const string NIBBLE_DICTIONARY = "0123456789abcdef";    
		public char encodeNibble( uint8_t nibble )
		{
			return NIBBLE_DICTIONARY[ nibble & 0x0F ];	
		}

		public string encodeByte( uint8_t byte_val )
		{
			System.Text.StringBuilder result = new System.Text.StringBuilder( 2 );
			result.Append( encodeNibble( Conversions.highNibble( byte_val ) ) );
			result.Append( encodeNibble( Conversions.lowNibble( byte_val ) ) );
			return result.ToString( );
		}

		public	int	  decodeByte( char firstChar, char secondChar )
		{
			int firstPos = NIBBLE_DICTIONARY.IndexOf( firstChar );
			int secondPos = NIBBLE_DICTIONARY.IndexOf( secondChar );
			if( firstPos >= 0 && secondPos >= 0 ) {
				return (int)Conversions.buildByte( (uint8_t)firstPos, (uint8_t)secondPos );
			} else
				return -1;
		}

	}

	public class Encryption
	{
		protected uint32_t key_;

		public Encryption( uint32_t key )
		{
			key_ = key;
		}

		public uint8_t encodeByte( uint16_t idx, uint8_t byte_val )
		{
			return byte_val;
		}

		public uint8_t decodeByte( uint16_t idx, uint8_t byte_val )
		{
			return byte_val;
		}

		public uint32_t	Key {
			get	{ return key_; }
			set { key_ = value; }
		}

	}

	public class InvalidFormatException 	: FormatException
	{

	}

	public class BodyErrorException     	: FormatException
	{

	}

	public class DecryptionFailedException  : FormatException
	{

	}

	public class IntegrityBrokenException	: FormatException
	{

	}

	public class BufferEmptyException : FormatException { }


	public class EncodedMessageReader
	{
		protected	string 		body_;
		protected	uint16_t	position_;

		public EncodedMessageReader( string body )
		{
			body_ = body;
			position_ = 0;
		}

		public	string		Body		{ get { return body_; } }

		public	uint16_t	Position	{ get { return position_; } }

		public	uint16_t	Length		{ get { return (uint16_t) body_.Length; } }

		public	bool		AtEnd		{ get { return position_ >= body_.Length; } }

		public	bool		HasMore		{ get { return position_ < body_.Length; } }

		public	char		CurrentChar	{ get { return position_ + 0 >= body_.Length ? '\0' : body_[ position_ + 0 ]; } }

		public	char		NextChar	{ get { return position_ + 1 >= body_.Length ? '\0' : body_[ position_ + 1 ]; } }

		public	bool		skipSpaces( )
		{
			while( HasMore && Char.IsWhiteSpace( CurrentChar ) ) {
				position_++;
			}
			return HasMore;
		}

		public	bool		skipChars( uint16_t count )
		{
			position_ += count;
			return HasMore;
		}

		public	bool		isStartsWith( string prefix )
		{
			skipSpaces( );
			if( body_.Substring( position_, prefix.Length ).Equals( prefix ) ) {
				position_ += (uint16_t) prefix.Length;
				skipSpaces( );
				return true;
			} else
				return false;
		}
	}

	public interface ISerializable 
	{
		void	load( Message.Reader reader )	;
		void	save( Message.Writer writer )	;
	}

	public class Message
	{
		public const uint16_t	BODY_SIZE = 256;
		public const string 	MESSAGE_HEADER = "<msg>";
		public const string 	MESSAGE_FOOTER = "</msg>";
		public const string 	NEW_LINE = "\n\r";
		public const uint8_t	CRC_INITIAL_SEED = 0xAB;
		public const float		FLOAT_FACTOR = 1000.0f;
		public const int32_t	FLOAT_NULL_VALUE  = Int32.MaxValue;
		public const int32_t	INT32_NULL_VALUE  = Int32.MaxValue;
		public const int16_t	INT16_NULL_VALUE  = Int16.MaxValue;
		public const uint32_t	UINT32_NULL_VALUE = UInt32.MaxValue;
		public const uint16_t	UINT16_NULL_VALUE = UInt16.MaxValue;
		public const uint8_t	UINT8_NULL_VALUE  = Byte.MaxValue;

		protected uint8_t[] body_ = new uint8_t[ BODY_SIZE ];
		protected uint16_t len_ = 0;

		public		uint8_t[]	Body 	{ get { return body_; }}
		public		uint16_t	Length	{ get { return len_;  }}

		public Message( )
		{
		}

		public	void	reset( )
		{
			len_ = 0;
		}

		public	void	_write( uint8_t val )
		{
			if( len_ < BODY_SIZE ) {
				body_[ len_ ] = val;
				len_++;
			} else {
				throw new System.FormatException( "Message overflow" );
			}
		}

		public string encodeMessage( Encryption encryption, Encoding encoding )
		{
			System.Text.StringBuilder result = new System.Text.StringBuilder( len_ * 2 + 20 );
			result.Append( MESSAGE_HEADER );
			result.Append( NEW_LINE );
			uint16_t charCount = 0;
			CRC8	crcCalc = new CRC8( CRC_INITIAL_SEED );
			for( uint16_t idx = 0; idx <= len_; idx++ ) {
				uint8_t byte_val = body_[ idx ];
				if( idx != len_ )
					crcCalc.updateByte( byte_val );
				else
					byte_val = crcCalc.Value;
				
				uint8_t encrypted_val = encryption.encodeByte( idx, byte_val );
				uint8_t highNibble = Conversions.highNibble( encrypted_val );
				uint8_t lowNibble = Conversions.lowNibble( encrypted_val );
				char	firstChar = encoding.encodeNibble( highNibble );
				char	secondChar = encoding.encodeNibble( lowNibble );

				result.Append( firstChar );
				result.Append( secondChar );
				charCount += 2;
				if( charCount >= 40 ) {
					charCount = 0;
					result.Append( NEW_LINE );
				}
			}
			if( charCount != 0 )
				result.Append( NEW_LINE );
			result.Append( MESSAGE_FOOTER );
			return result.ToString( );
		}

		public Writer startWriting( )
		{
			reset( );
			return new Writer( this );
		}

		public bool	 decodeMessage( Encryption encryption, Encoding encoding, string msgBody )
		{
			reset( );
			EncodedMessageReader	reader = new EncodedMessageReader( msgBody );
			if( reader.isStartsWith( MESSAGE_HEADER ) == false )
				throw new InvalidFormatException( );		

			bool	footerFound = false;
			while( reader.skipSpaces( ) ) {
				if( reader.isStartsWith( MESSAGE_FOOTER ) ) {
					footerFound = true;
					break;
				}
				char	firstChar = reader.CurrentChar;
				char	secondChar = reader.NextChar;
				Writer	writer = startWriting( );
				int byte_val = encoding.decodeByte( firstChar, secondChar );
				if( byte_val >= 0 ) {
					uint8_t decrypted_val = encryption.decodeByte( len_, (uint8_t) byte_val );
					writer.writeByte( decrypted_val );
				} else {
					throw new BodyErrorException( );
				}
			}

			if( footerFound == false )
				throw new InvalidFormatException( );

			if( len_ < 1 )
				throw new BodyErrorException( );

			uint8_t crcStored = body_[ len_ - 1 ];
			len_--;

			// Now check CRC8
			CRC8 crcCalc = new CRC8( CRC_INITIAL_SEED );
			crcCalc.updateBytes( body_, 0, len_ );
			uint8_t crcEstimated = crcCalc.Value;

			if( crcStored != crcEstimated )
				throw new IntegrityBrokenException( );
			
			return len_ > 0;
		}

		public class Writer
		{
			protected	Message message_;

			public Writer( Message message )
			{
				message_ = message	;
			}

			public	void	writeByte( uint8_t byte_val )
			{
				message_._write( byte_val );
			}

			public 	void	writeByteNull( uint8_t? byte_val )
			{
				if( byte_val.HasValue )
					writeByte( byte_val.Value );
				else
					writeByte( UINT8_NULL_VALUE );
			}

			public	void	writeBool( bool val )
			{
				writeByte( val ? (uint8_t)0x01 : (uint8_t)0x00 );
			}

			public	void	writeBoolNull( bool? val )
			{
				if( val.HasValue )
					writeBool( val.Value );
				else
					writeByte( (uint8_t) 0xFF );			}

			public 	void	writeWord( uint16_t word_val )
			{
				writeByte( Conversions.highByte( word_val ) );
				writeByte( Conversions.lowByte( word_val ) );
			}

			public 	void	writeWordNull( uint16_t? word_val )
			{
				if( word_val.HasValue )
					writeWord( word_val.Value );
				else
					writeWord( UINT16_NULL_VALUE );
			}

			public 	void	writeDword( uint32_t dword_val )
			{
				writeWord( Conversions.highWord( dword_val ) );
				writeWord( Conversions.lowWord( dword_val ) );
			}

			public 	void	writeDwordNull( uint32_t? dword_val )
			{
				if( dword_val.HasValue )
					writeDword( dword_val.Value );
				else
					writeDword( UINT32_NULL_VALUE );
			}

			public 	void	writeShort( int16_t word_val )
			{
				writeByte( Conversions.highByte( (uint16_t) word_val ) );
				writeByte( Conversions.lowByte( (uint16_t) word_val ) );
			}

			public 	void	writeShortNull( int16_t? short_val )
			{
				if( short_val.HasValue )
					writeShort( short_val.Value );
				else
					writeShort( INT16_NULL_VALUE );
			}

			public 	void	writeLong( int32_t dword_val )
			{
				writeWord( Conversions.highWord( (uint32_t) dword_val ) );
				writeWord( Conversions.lowWord( (uint32_t) dword_val ) );
			}

			public 	void	writeLongNull( int32_t? long_val )
			{
				if( long_val.HasValue )
					writeLong( long_val.Value );
				else
					writeLong( INT32_NULL_VALUE );
			}

			public	void	writeFloat( float float_val )
			{
				int32_t long_val = (int32_t) (float_val / FLOAT_FACTOR);
				writeLong( long_val );
			}

			public	void	writeFloatNull( float? float_val )
			{
				int32_t long_val = float_val.HasValue ? (int32_t) (float_val / FLOAT_FACTOR) : FLOAT_NULL_VALUE;
				writeLong( long_val );
			}

			public  void	writeString( string str_val )
			{
				uint16_t len = (uint16_t) str_val.Length;
				if( len < 0xFF )
					writeByte( (uint8_t) len );
				else
				{ 
					writeByte( (uint8_t) 0xFF ); 
					writeWord( len ); 
				}
				for( uint16_t idx = 0; idx < len; idx++ )
					writeByte( (uint8_t) str_val[ idx ] );
			}


			public void		writeList<T>( List<T> list ) where T:ISerializable
			{
				writeWord( (uint16_t) list.Count );
				foreach( T item in list )
					item.save( this );
			}

			public void		writeDictionary<TKey,TValue>( Dictionary<TKey,TValue> dict ) where TKey:ISerializable where TValue:ISerializable
			{
				uint16_t elem_count = (uint16_t) dict.Count;
				writeWord( elem_count );
				foreach(KeyValuePair<TKey, TValue> entry in dict ) 
				{
					entry.Key.save( this );
					entry.Value.save( this );
				}
			}

		
		}

		public class Reader
		{
			protected	uint16_t	position_;
			protected	Message		message_;

			public	uint16_t	Position 	{ get { return position_; }}
			public	Message		Msg  		{ get { return message_; }}

			public Reader( Message msg )
			{
				position_ = 0;
				message_ = msg;
			}

			public	bool		AtEnd		{ get { return position_ >= Msg.Length; }}
			public	bool		hasByte( )	{ return position_ + sizeof( uint8_t ) <= Msg.Length;  }
			public	bool		hasWord( )	{ return position_ + sizeof( uint16_t ) <= Msg.Length; }
			public	bool		hasDword( )	{ return position_ + sizeof( uint32_t ) <= Msg.Length; }
			public	bool		hasMore( uint16_t bytes )	{ return position_ + bytes <= Msg.Length; }

			public	uint8_t		readByte( )	
			{
				if( hasByte( ))
				{
					uint8_t byte_val = Msg.Body[ position_ ];
					position_++;
					return byte_val;
				} else {
					throw new BufferEmptyException( );
				}
			}

			public	uint8_t?		readByteNull( )	
			{
				uint8_t	result = readByte( );
				if( result == UINT8_NULL_VALUE )
					return null;
				else
					return result;
			}

			public	bool			readBool( )
			{
				uint8_t byte_val = readByte( );
				if( byte_val >= 2 )
					throw new InvalidFormatException( );
				return byte_val != 0;
			}

			public	bool?			readBoolNull( )
			{
				uint8_t byte_val = readByte( );
				switch( byte_val )
				{
				case	0:
					return false;
				case 1:
					return true;
				case (uint8_t)0xFF:
					return null;
				default:
					throw new InvalidFormatException( );
				}
			}

			public	uint16_t		readWord( )	
			{
				if( hasWord( ))
				{
					uint8_t highByte = Msg.Body[ position_ + 0 ];
					uint8_t lowByte = Msg.Body[ position_ + 1 ];
					position_+=2;
					return Conversions.buildWord( highByte, lowByte );
				} else {
					throw new BufferEmptyException( );
				}
			}

			public	uint16_t?		readWordNull( )	
			{
				uint16_t	result = readWord( );
				if( result == UINT16_NULL_VALUE )
					return null;
				else
					return result;
			}

			public	uint32_t		readDword( )	
			{
				if( hasDword( ))
				{
					uint16_t highWord = readWord( );
					uint16_t lowWord  = readWord( );
					return Conversions.buildDword( highWord, lowWord );
				} else {
					throw new BufferEmptyException( );
				}
			}

			public	uint32_t?		readDwordNull( )	
			{
				uint32_t	result = readDword( );
				if( result == UINT32_NULL_VALUE )
					return null;
				else
					return result;
			}

			public	int16_t		readShort( )
			{
				return (int16_t) readWord( );
			}

			public	int16_t?	readShortNull( )	
			{
				int16_t	result = readShort( );
				if( result == INT16_NULL_VALUE )
					return null;
				else
					return result;
			}

			public	int32_t		readLong( )
			{
				return (int32_t) readDword( );
			}

			public	int32_t?	readLongNull( )	
			{
				int32_t	result = readLong( );
				if( result == INT32_NULL_VALUE )
					return null;
				else
					return result;
			}

			public	float		readFloat( )
			{
				int32_t int_val = readLong( );
				return int_val / (float)FLOAT_FACTOR;
			}

			public	float?		readFloatNull( )
			{
				int32_t int_val = readLong( );
				if( int_val == FLOAT_NULL_VALUE )
					return null;
				else
					return int_val / (float)FLOAT_FACTOR;
			}

			public	string		readString( )
			{
				uint8_t shortLen;
				uint16_t len;

				shortLen = readByte( );
				if( shortLen == (uint8_t) 0xFF )
					len = readWord( );
				else
					len = shortLen;

				System.Text.StringBuilder	result = new System.Text.StringBuilder( len );
				for( uint8_t idx = 0; idx < len; idx ++ )
				{
					uint8_t byte_val = readByte( );
					char	ch = (char) byte_val;
					result.Append( ch );
				}
				return result.ToString( );
			}

			public void	readList<T>( List<T> list ) where T:ISerializable
			{
				list.Clear( );
				uint16_t elem_count = readWord( );
				list.Capacity = elem_count;
				for( uint16_t idx = 0; idx < elem_count; idx ++ )
				{
					T item = default(T);
					item.load( this );
				}
			}

			public void	readDictionary<TKey,TValue>( Dictionary<TKey,TValue> dict ) where TKey:ISerializable where TValue:ISerializable
			{
				dict.Clear( );
				uint16_t elem_count = readWord( );
				for( uint16_t idx = 0; idx < elem_count; idx ++ )
				{
					TKey item_key = default(TKey);
					item_key.load( this );
					TValue	item_value = default(TValue);
					item_value.load( this );
					dict.Add( item_key, item_value );
				}
			}

		}
	}

	public static class LoadSaveExtensions
	{
		public static void	load( this     bool   arg, Message.Reader reader )  { arg = reader.readBool( ); }
		public static void	load( this  uint8_t   arg, Message.Reader reader )  { arg = reader.readByte( ); }
		public static void	load( this uint16_t   arg, Message.Reader reader )  { arg = reader.readWord( ); }
		public static void	load( this  int16_t   arg, Message.Reader reader )  { arg = reader.readShort( ); }
		public static void	load( this uint32_t   arg, Message.Reader reader )  { arg = reader.readDword( ); }
		public static void	load( this  int32_t   arg, Message.Reader reader )  { arg = reader.readLong( ); }
		public static void	load( this    float   arg, Message.Reader reader )  { arg = reader.readFloat( ); }
		public static void	load( this     bool?  arg, Message.Reader reader )  { arg = reader.readBoolNull( ); }
		public static void	load( this  uint8_t?  arg, Message.Reader reader )  { arg = reader.readByteNull( ); }
		public static void	load( this uint16_t?  arg, Message.Reader reader )  { arg = reader.readWordNull( ); }
		public static void	load( this  int16_t?  arg, Message.Reader reader )  { arg = reader.readShortNull( ); }
		public static void	load( this uint32_t?  arg, Message.Reader reader )  { arg = reader.readDwordNull( ); }
		public static void	load( this  int32_t?  arg, Message.Reader reader )  { arg = reader.readLongNull( ); }
		public static void	load( this   float?   arg, Message.Reader reader )  { arg = reader.readFloatNull( ); }
		public static void	load( this   string   arg, Message.Reader reader )  { arg = reader.readString( ); }
		public static void	load<T>( this List<T> arg, Message.Reader reader )  where T:ISerializable { reader.readList( arg ); }
		public static void	load<TKey,TValue>( this Dictionary<TKey,TValue> arg, Message.Reader reader )  where TKey:ISerializable where TValue:ISerializable { reader.readDictionary( arg ); }
		public static void  load<T>( this Nullable<T> arg, Message.Reader reader ) where T:struct, ISerializable    
		{
			bool isNull = reader.readBool( );
			if( isNull )
				arg = null;
			else
				( (T) arg ).load( reader );
		}


		public static void	save( this     bool  arg, Message.Writer writer )  { writer.writeBool( arg ); }
		public static void	save( this  uint8_t  arg, Message.Writer writer )  { writer.writeByte( arg ); }
		public static void	save( this uint16_t  arg, Message.Writer writer )  { writer.writeWord( arg ); }
		public static void	save( this  int16_t  arg, Message.Writer writer )  { writer.writeShort( arg ); }
		public static void	save( this uint32_t  arg, Message.Writer writer )  { writer.writeDword( arg ); }
		public static void	save( this  int32_t  arg, Message.Writer writer )  { writer.writeLong( arg ); }
		public static void	save( this    float  arg, Message.Writer writer )  { writer.writeFloat( arg ); }
		public static void	save( this     bool? arg, Message.Writer writer )  { writer.writeBoolNull( arg ); }
		public static void	save( this  uint8_t? arg, Message.Writer writer )  { writer.writeByteNull( arg ); }
		public static void	save( this uint16_t? arg, Message.Writer writer )  { writer.writeWordNull( arg ); }
		public static void	save( this  int16_t? arg, Message.Writer writer )  { writer.writeShortNull( arg ); }
		public static void	save( this uint32_t? arg, Message.Writer writer )  { writer.writeDwordNull( arg ); }
		public static void	save( this  int32_t? arg, Message.Writer writer )  { writer.writeLongNull( arg ); }
		public static void	save( this    float? arg, Message.Writer writer )  { writer.writeFloatNull( arg ); }
		public static void	save( this   string  arg, Message.Writer writer )  { writer.writeString( arg ); }
		public static void	save<T>( this List<T> arg, Message.Writer writer )  where T:ISerializable { writer.writeList( arg ); }
		public static void	save<TKey,TValue>( this Dictionary<TKey,TValue> arg, Message.Writer writer )  where TKey:ISerializable where TValue:ISerializable { writer.writeDictionary( arg ); }

	}

	public struct Flagged<T> where T:ISerializable 
	{
		public 	T		Value { get; set; }
		public	bool	Flag  { get; set; }

		public 	void	load( Message.Reader reader )
		{
			Value.load( reader );
			Flag = reader.readBool( );
		}
		public	void	save( Message.Writer writer )
		{
			Value.save( writer );
			writer.writeBool( Flag );
		}
	}

	public struct FlaggedFloat 
	{
		public 	float	Value { get; set; }
		public	bool	Flag  { get; set; }

		public 	void	load( Message.Reader reader )
		{
			Value = reader.readFloat( );
			Flag = reader.readBool( );
		}
		public	void	save( Message.Writer writer )
		{
			writer.writeFloat( Value );
			writer.writeBool( Flag );
		}
	}

	public struct FlaggedInt16 
	{
		public 	int16_t	Value { get; set; }
		public	bool	Flag  { get; set; }

		public 	void	load( Message.Reader reader )
		{
			Value = reader.readShort( );
			Flag = reader.readBool( );
		}
		public	void	save( Message.Writer writer )
		{
			writer.writeShort( Value );
			writer.writeBool( Flag );
		}
	}

	public struct FlaggedUInt16 
	{
		public 	uint16_t	Value { get; set; }
		public	bool		Flag  { get; set; }

		public 	void	load( Message.Reader reader )
		{
			Value = reader.readWord( );
			Flag = reader.readBool( );
		}
		public	void	save( Message.Writer writer )
		{
			writer.writeWord( Value );
			writer.writeBool( Flag );
		}
	}

	public struct FlaggedInt32
	{
		public 	int32_t	Value { get; set; }
		public	bool	Flag  { get; set; }

		public 	void	load( Message.Reader reader )
		{
			Value = reader.readLong( );
			Flag = reader.readBool( );
		}
		public	void	save( Message.Writer writer )
		{
			writer.writeLong( Value );
			writer.writeBool( Flag );
		}
	}

	public struct FlaggedUInt32 
	{
		public 	uint32_t	Value { get; set; }
		public	bool		Flag  { get; set; }

		public 	void	load( Message.Reader reader )
		{
			Value = reader.readDword( );
			Flag = reader.readBool( );
		}
		public	void	save( Message.Writer writer )
		{
			writer.writeDword( Value );
			writer.writeBool( Flag );
		}
	}


}


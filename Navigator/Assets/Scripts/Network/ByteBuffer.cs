using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using UnityEngine;

namespace VEXNetwork
{
    /*
     * Скрипт отвечает за преобразование байтов в разные типы и наоборот и работу с буфером
     */
    public struct ByteBuffer : IDisposable
    {
        /// <summary>
        /// массив ячеек, в котором будут храниться данные
        /// </summary>
        public byte[] Data;
        /// <summary>
        /// указывает сколько ячеек в буфере заполнено данными
        /// </summary>
        public int Head;

        #region CONSTRUCTORS 
        /// <summary>
        /// конструктор по умолчанию
        /// </summary>
        /// <param name="initialSize"></param>
        public ByteBuffer(int initialSize = 4)
        {
            this.Data = new byte[initialSize];
            this.Head = 0;
        }

        /// <summary>
        /// конструктор с записью данных
        /// </summary>
        /// <param name="bytes"></param>
        public ByteBuffer(byte[] bytes)
        {
            this.Data = bytes;
            this.Head = 0;
        }
        #endregion

        /// <summary>
        /// очистка
        /// </summary>
        public void Dispose()
        {
            this.Data = (byte[])null;
            this.Head = 0;
        }

        /// <summary>
        /// преобразование в массив
        /// </summary>
        /// <returns></returns>
        public byte[] ToArray()
        {
            byte[] numArray = new byte[this.Head];
            Buffer.BlockCopy((Array)this.Data, 0, (Array)numArray, 0, this.Head);
            return numArray;
        }

        /// <summary>
        /// преобразование в пакет
        /// </summary>
        public byte[] ToPacket()
        {
            byte[] numArray = new byte[4 + this.Head];
            //записываем в заголовок пакета сколько отправляем байт
            Buffer.BlockCopy((Array)BitConverter.GetBytes(this.Head), 0, (Array)numArray, 0, 4);
            //записываем сами данные из буфера
            Buffer.BlockCopy((Array)this.Data, 0, (Array)numArray, 4, this.Head);
            return numArray;
        }

        /// <summary>
        /// проверка размера буфера и увеличение его в зависимости от размера записываемых данных
        /// </summary>
        /// <param name="length"></param>
        private void CheckSize(int length)
        {
            int num = this.Data.Length;
            if (length + this.Head < num)
                return;
            if (num < 4)
                num = 4;
            int length1 = num * 2;
            while (length + this.Head >= length1)
                length1 *= 2;
            byte[] numArray = new byte[length1];
            Buffer.BlockCopy((Array)this.Data, 0, (Array)numArray, 0, this.Head);
            this.Data = numArray;
        }

        #region READMETHODS
        /// <summary>
        /// получение с буфера массива байт указанной длины
        /// </summary>
        /// <param name="size"></param>
        /// <returns></returns>
        public byte[] ReadBlock(int size)
        {
            if (size <= 0 || this.Head + size > this.Data.Length)
                return new byte[0];
            byte[] numArray = new byte[size];
            Buffer.BlockCopy((Array)this.Data, this.Head, (Array)numArray, 0, size);
            this.Head += size;
            return numArray;
        }

        /// <summary>
        /// чтение Object
        /// </summary>
        /// <returns></returns>
        public object ReadObject()
        {
            //сможем ли мы прочитать размер объекта
            if (this.Head + 4 > this.Data.Length)
                return (object)null;
            //читаем размер объекта
            int int32 = BitConverter.ToInt32(this.Data, this.Head);
            this.Head += 4;
            //есть ли в непрочитанном буфере количество байт указанное в размере объекта
            if (int32 <= 0 || this.Head + int32 > this.Data.Length)
                return (object)null;
            MemoryStream memoryStream = new MemoryStream();
            memoryStream.SetLength((long)int32);
            memoryStream.Read(this.Data, this.Head, int32);
            this.Head += int32;
            object obj = new BinaryFormatter().Deserialize((Stream)memoryStream);
            memoryStream.Dispose();
            return obj;
        }

        /// <summary>
        /// чтение массива байт
        /// </summary>
        /// <returns></returns>
        public byte[] ReadBytes()
        {
            if (this.Head + 4 > this.Data.Length)
                return new byte[0];
            int int32 = BitConverter.ToInt32(this.Data, this.Head);
            this.Head += 4;
            if (int32 <= 0 || this.Head + int32 > this.Data.Length)
                return new byte[0];
            byte[] numArray = new byte[int32];
            Buffer.BlockCopy((Array)this.Data, this.Head, (Array)numArray, 0, int32);
            this.Head += int32;
            return numArray;
        }

        public string ReadString()
        {
            if (this.Head + 4 > this.Data.Length)
                return "";
            int int32 = BitConverter.ToInt32(this.Data, this.Head);
            this.Head += 4;
            if (int32 <= 0 || this.Head + int32 > this.Data.Length)
                return "";
            string str = Encoding.UTF8.GetString(this.Data, this.Head, int32);
            this.Head += int32;
            return str;
        }

        public char ReadChar()
        {
            if (this.Head + 2 > this.Data.Length)
                return char.MinValue;
            int num = (int)BitConverter.ToChar(this.Data, this.Head);
            this.Head += 2;
            return (char)num;
        }

        public byte ReadByte()
        {
            if (this.Head + 1 > this.Data.Length)
                return 0;
            int num = (int)this.Data[this.Head];
            ++this.Head;
            return (byte)num;
        }

        public bool ReadBoolean()
        {
            if (this.Head + 1 > this.Data.Length)
                return false;
            int num = BitConverter.ToBoolean(this.Data, this.Head) ? 1 : 0;
            ++this.Head;
            return (uint)num > 0U;
        }

        public short ReadInt16()
        {
            if (this.Head + 2 > this.Data.Length)
                return 0;
            int int16 = (int)BitConverter.ToInt16(this.Data, this.Head);
            this.Head += 2;
            return (short)int16;
        }

        public ushort ReadUInt16()
        {
            if (this.Head + 2 > this.Data.Length)
                return 0;
            int uint16 = (int)BitConverter.ToUInt16(this.Data, this.Head);
            this.Head += 2;
            return (ushort)uint16;
        }

        public int ReadInt32()
        {
            if (this.Head + 4 > this.Data.Length)
                return 0;
            int int32 = BitConverter.ToInt32(this.Data, this.Head);
            this.Head += 4;
            return int32;
        }

        public uint ReadUInt32()
        {
            if (this.Head + 4 > this.Data.Length)
                return 0;
            int uint32 = (int)BitConverter.ToUInt32(this.Data, this.Head);
            this.Head += 4;
            return (uint)uint32;
        }

        public float ReadSingle()
        {
            if (this.Head + 4 > this.Data.Length)
                return 0.0f;
            double single = (double)BitConverter.ToSingle(this.Data, this.Head);
            this.Head += 4;
            return (float)single;
        }

        public long ReadInt64()
        {
            if (this.Head + 8 > this.Data.Length)
                return 0;
            long int64 = BitConverter.ToInt64(this.Data, this.Head);
            this.Head += 8;
            return int64;
        }

        public ulong ReadUInt64()
        {
            if (this.Head + 8 > this.Data.Length)
                return 0;
            long uint64 = (long)BitConverter.ToUInt64(this.Data, this.Head);
            this.Head += 8;
            return (ulong)uint64;
        }

        public double ReadDouble()
        {
            if (this.Head + 8 > this.Data.Length)
                return 0.0;
            double num = BitConverter.ToDouble(this.Data, this.Head);
            this.Head += 8;
            return num;
        }

        public Vector3 ReadVector3()
        {
            Vector3 res = new Vector3
            {
                x = ReadSingle(),
                y = ReadSingle(),
                z = ReadSingle()
            };
            return res;
        }

        public Quaternion ReadQuaternion()
        {
            Quaternion res = new Quaternion
            {
                x = ReadSingle(),
                y = ReadSingle(),
                z = ReadSingle(),
                w = ReadSingle()
            };
            return res;
        }
        #endregion

        #region WRITEMETHODS
        /// <summary>
        /// запись в буфер массива байт и добавление к head размера этого массива
        /// </summary>
        /// <param name="bytes"></param>
        public void WriteBlock(byte[] bytes)
        {
            this.CheckSize(bytes.Length);
            Buffer.BlockCopy((Array)bytes, 0, (Array)this.Data, this.Head, bytes.Length);
            this.Head += bytes.Length;
        }

        /// <summary>
        /// запись в буфер массива байт и добавление к head размера этого массива со сдвигом
        /// </summary>
        /// <param name="bytes"></param>
        /// <param name="offset"></param>
        /// <param name="size"></param>
        public void WriteBlock(byte[] bytes, int offset, int size)
        {
            this.CheckSize(size);
            Buffer.BlockCopy((Array)bytes, offset, (Array)this.Data, this.Head, size);
            this.Head += size;
        }

        /// <summary>
        /// запись в буфер Object
        /// </summary>
        /// <param name="value"></param>
        public void WriteObject(object value)
        {
            MemoryStream memoryStream = new MemoryStream();
            new BinaryFormatter().Serialize((Stream)memoryStream, value);
            byte[] array = memoryStream.ToArray();
            int length = array.Length;
            memoryStream.Dispose();
            this.WriteBlock(BitConverter.GetBytes(length));
            this.WriteBlock(array);
        }

        /// <summary>
        /// запись массива байт (сколько байт затем сам массив)
        /// </summary>
        /// <param name="value"></param>
        /// <param name="offset"></param>
        /// <param name="size"></param>
        public void WriteBytes(byte[] value, int offset, int size)
        {
            this.WriteBlock(BitConverter.GetBytes(size));
            this.WriteBlock(value, offset, size);
        }

        public void WriteBytes(byte[] value)
        {
            this.WriteBlock(BitConverter.GetBytes(value.Length));
            this.WriteBlock(value);
        }

        /// <summary>
        /// запись строки (сколько символов затем сама строка в виде массива байт)
        /// </summary>
        /// <param name="value"></param>
        public void WriteString(string value)
        {
            if (value == null)
            {
                this.WriteInt32(0);
            }
            else
            {
                byte[] bytes = Encoding.UTF8.GetBytes(value);
                this.WriteInt32(bytes.Length);
                this.WriteBlock(bytes);
            }
        }

        /// <summary>
        /// запись char (только само значение)
        /// </summary>
        /// <param name="value"></param>
        public void WriteChar(char value)
        {
            this.WriteBlock(BitConverter.GetBytes(value));
        }

        /// <summary>
        /// запись одного байта в буфер
        /// </summary>
        /// <param name="value"></param>
        public void WriteByte(byte value)
        {
            this.CheckSize(1);
            this.Data[this.Head] = value;
            ++this.Head;
        }

        /// <summary>
        /// запись bool в буфер
        /// </summary>
        /// <param name="value"></param>
        public void WriteBoolean(bool value)
        {
            this.WriteBlock(BitConverter.GetBytes(value));
        }

        /// <summary>
        /// запись short в буфер
        /// </summary>
        /// <param name="value"></param>
        public void WriteInt16(short value)
        {
            this.WriteBlock(BitConverter.GetBytes(value));
        }

        /// <summary>
        /// запись ushort в буфер
        /// </summary>
        /// <param name="value"></param>
        public void WriteUInt16(ushort value)
        {
            this.WriteBlock(BitConverter.GetBytes(value));
        }

        /// <summary>
        /// запись int в буфер
        /// </summary>
        /// <param name="value"></param>
        public void WriteInt32(int value)
        {
            this.WriteBlock(BitConverter.GetBytes(value));
        }

        public void WriteUInt32(uint value)
        {
            this.WriteBlock(BitConverter.GetBytes(value));
        }

        public void WriteSingle(float value)
        {
            this.WriteBlock(BitConverter.GetBytes(value));
        }

        public void WriteInt64(long value)
        {
            this.WriteBlock(BitConverter.GetBytes(value));
        }

        public void WriteUInt64(ulong value)
        {
            this.WriteBlock(BitConverter.GetBytes(value));
        }

        public void WriteDouble(double value)
        {
            this.WriteBlock(BitConverter.GetBytes(value));
        }

        public void WriteVector3(Vector3 value)
        {
            WriteSingle(value.x);
            WriteSingle(value.y);
            WriteSingle(value.z);
        }

        public void WriteQuternion(Quaternion value)
        {
            WriteSingle(value.x);
            WriteSingle(value.y);
            WriteSingle(value.z);
            WriteSingle(value.w);
        }
        #endregion
    }
}

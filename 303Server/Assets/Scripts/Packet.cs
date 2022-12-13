using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;


public class Packet
{
    private List<byte> buffer;
    private byte[] readableBuffer;
    private int readPosition;


    public Packet()
    {
        buffer = new List<byte>();
        readPosition = 0; // reset read position
    }


    public Packet(int id)
    {
        buffer = new List<byte>();
        readPosition = 0; // reset read position

        Write(id); //then write message id
    }


    public Packet(byte[] data)
    {
        buffer = new List<byte>();
        readPosition = 0; // reset read position

        SetBytes(data); //set initile data 
    }



    public void SetBytes(byte[] data)
    {
        Write(data);
        readableBuffer = buffer.ToArray();
    }

    public void WriteLength()
    {
        buffer.InsertRange(0, BitConverter.GetBytes(buffer.Count));
    }


    public byte[] ToArray()
    {
        return buffer.ToArray();
    }


    public int Length()
    {
        return buffer.Count;
    }


    public int UnreadLength()
    {
        return Length() - readPosition;
    }

    public void Reset()
    {
        //Reset member Variables to thier defualt
        buffer.Clear();
        readableBuffer = null;
        readPosition = 0;
    }

    public void Write(byte[] value)
    {
        buffer.AddRange(value);
    }

    public void Write(Color color)
    {
        Write(color.r);
        Write(color.g);
        Write(color.b);
        Write(color.a);
    }

    public void Write(int value)
    {
        buffer.AddRange(BitConverter.GetBytes(value));
    }


    public void Write(float value)
    {
        buffer.AddRange(BitConverter.GetBytes(value));
    }

    public void Write(string value)
    {
        Write(value.Length);
        buffer.AddRange(Encoding.ASCII.GetBytes(value));
    }

    public void Write(Vector3 value)
    {
        Write(value.x);
        Write(value.y);
        Write(value.z);
    }

    public void Write(Quaternion value)
    {
        //Write the four float values of a Quaternion
        Write(value.x);
        Write(value.y);
        Write(value.z);
        Write(value.w);
    }

    public void Write(bool _value)
    {
        buffer.AddRange(BitConverter.GetBytes(_value));
    }

    public void InsertInt(int value)
    {
        //Insert an int at the begining of the buffer
        buffer.InsertRange(0, BitConverter.GetBytes(value));
    }

    public byte[] ReadBytes(int length)
    {
        if (buffer.Count > readPosition) // if data can be read from 
        {
            byte[] value = buffer.GetRange(readPosition, length).ToArray();
            readPosition += length;
            return value;
        }
        else
        {
            throw new Exception("Could not read byte");
        }
    }



    public Color ReadColor()
    {
        if (buffer.Count > readPosition) // if data can be read from
        {
            float[] values = new float[4];

            for (int i = 0; i < 4; i++)
            {
                float value = BitConverter.ToSingle(readableBuffer, readPosition);
                values[i] = value;
                readPosition += 4;
            }


            return new Color(values[0], values[1], values[2], values[3]);
        }
        else
        {
            throw new Exception("Could not read Color");
        }
    }


    public int ReadInt()
    {
        if (buffer.Count > readPosition) // if data can be read from
        {
            int value = BitConverter.ToInt32(readableBuffer, readPosition);
            readPosition += 4;
            return value;

        }
        else
        {
            throw new Exception("Could not read int");
        }
    }

    public float ReadFloat()
    {
        if (buffer.Count > readPosition) // if data can be read from
        {
            float value = BitConverter.ToSingle(readableBuffer, readPosition);

            readPosition += 4;

            return value;
        }
        else
        {
            throw new Exception("Could not read float");
        }
    }
    public bool ReadBool()
    {
        if (buffer.Count > readPosition) // if data can be read from
        {

            bool value = BitConverter.ToBoolean(readableBuffer, readPosition);

            readPosition += 1;

            return value;
        }
        else
        {
            throw new Exception("Could not read bool");
        }
    }


    public string ReadString()
    {
        try
        {
            int length = ReadInt();
            string value = Encoding.ASCII.GetString(readableBuffer, readPosition, length);
            readPosition += length;

            return value;
        }
        catch
        {
            throw new Exception("Could not read string");
        }
    }


    public Vector3 ReadVector3()
    {
        return new Vector3(ReadFloat(), ReadFloat(), ReadFloat());
    }

    public Quaternion ReadQuaternion()
    {
        return new Quaternion(ReadFloat(), ReadFloat(), ReadFloat(), ReadFloat());
    }


}

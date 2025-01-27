﻿using UnityEngine;
using System;
using System.Diagnostics;
using System.Threading;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.IO.Ports;

public class SerialManager
{
    private SerialPort _serialPort;
    private string defaultPortName = "COM7";
    private int BaudRate = 115200;
    private int ReadTimeout = 150;
    private int WriteTimeout = 150;
    private bool isStart = true;
    private bool isClassify = false;
    private bool isThread = false;

    private float xw = 0.0f, yw = 0.0f, zw = 0.0f;
    private float ax = 0.0f, ay = 0.0f, az = 0.0f;
    private float px = 0.0f, py = 0.0f, pz = 0.0f;

    private bool isax = false, isay = false, isaz = false;

    private bool bl, bo, br;

    private Thread _serialThread;

    public SerialManager()
    {
        _serialPort = new SerialPort();
        
        /*
        foreach(string str in SerialPort.GetPortNames())
        {
            UnityEngine.Debug.Log(string.Format("Port : {0}", str));
        }
        */
    }

    // set serial status
    private void SetSerialInformation()
    {
        _serialPort.PortName = defaultPortName;
        _serialPort.BaudRate = BaudRate;
        _serialPort.ReadTimeout = ReadTimeout;
        _serialPort.WriteTimeout = WriteTimeout;

        SetSerialThread();

    }
    public void SetSerialOpen()
    {
        if (_serialPort.IsOpen) return;

        SetSerialInformation();

        _serialPort.Open();

        SendStartMessage();

        ClassifyFunction();
    }
    public void SetSerialClose()
    {
        if (!_serialPort.IsOpen) return;

        if (_serialThread != null) _serialThread.Join();

        _serialPort.Close();
    }

    // thread
    private void SerialThread()
    {
        while (isThread)
            ClassifySerialValue();
    }
    private void SetSerialThread()
    {
        _serialThread = new Thread(SerialThread);
    }
    private void StartSerialThread()
    {
        if (isThread) return;

        _serialThread.Start();
        isThread = true;
    }
    public void StopSerialThread()
    {
        isThread = false;
    }

    // set default information before serial opened
    public void SetSerialPort(string PortName)
    {
        this.defaultPortName = PortName;
    }
    public void SetBaudRate(int BaudRate)
    {
        this.BaudRate = BaudRate;
    }
    public void SetReadTimeout(int ReadTimeout)
    {
        this.ReadTimeout = ReadTimeout;
    }
    public void SetWriteTimeout(int WriteTimeout)
    {
        this.WriteTimeout = WriteTimeout;
    }


    // called when get data through the serial
    private void ClassifyFunction()
    {
        if (!_serialPort.IsOpen) return;

        //if (!isStart) ClassifyHandType();
        //else
        //{
            if (!isThread) StartSerialThread();
        //}
    }
    private void ClassifySerialValue()
    {
        string data = "";
        string values = "";
        char character;

        try
        {
            data += (char)_serialPort.ReadChar();
            data += (char)_serialPort.ReadChar();
            switch (data)
            {
                case "00":
                case "01":
                case "10":
                case "11":
                    data += (char)_serialPort.ReadChar();
                    SetButtonValue(data);
                    break;
                case "GY":
                    character = (char)_serialPort.ReadChar();
                    values = _serialPort.ReadLine();
                    SetGyroValue(character, values);
                    break;
                case "AC":
                    character = (char)_serialPort.ReadChar();
                    values = _serialPort.ReadLine();
                    SetAccValue(character, values);
                    break;
                case "AN":
                    character = (char)_serialPort.ReadChar();
                    values = _serialPort.ReadLine();
                    SetAngValue(character, values);
                    break;
                case "PO":
                    character = (char)_serialPort.ReadChar();
                    values = _serialPort.ReadLine();
                    SetPosValue(character, values);
                    break;
                case "FI":
                    data = "" + _serialPort.ReadChar();
                    values = _serialPort.ReadLine();
                    //SetFinValue(values);
                    break;
                default:
                    values = data + _serialPort.ReadLine();
                    //UnityEngine.Debug.Log(values);
                    break;
            }
            // UnityEngine.Debug.Log("data : " + data + " values : " + values);
        }
        catch (System.Exception exception)
        {
            UnityEngine.Debug.Log(exception.Message);
            UnityEngine.Debug.Log("System.Exception in ClassifySerialValue ");
        }
    }

    // send message to arduino
    private void SendStartMessage()
    {
        if (!_serialPort.IsOpen) return;

        _serialPort.Write("1 1");
        // UnityEngine.Debug.Log("DMP");

        isClassify = true;
    }
    public void SendVibrationMessage(int strength)
    {
        if (!_serialPort.IsOpen) return;

        _serialPort.Write("VI" + strength);
        UnityEngine.Debug.Log("VI" + strength);
    }

    // set value received from the arduino
    private float SetStringValuesToFloatValue(string values)
    {
        string integer_part = "";
        string decimal_part = "";

        bool is_negative = false;
        bool is_there_decimal = false;

        foreach (char character in values)
        {
            if (character == '-') is_negative = true;
            else if (character == '.') is_there_decimal = true;
            else
            {
                if (is_there_decimal) decimal_part += character;
                else integer_part += character;
            }
        }

        return ((float)(Int32.Parse(integer_part) + (is_there_decimal ? Int32.Parse(decimal_part) * Math.Pow(10, decimal_part.Length * -1) : 0)) * ( is_negative ? -1 : 1 ));
    }
    private int SetBinaryValuesToIntValue(string values)
    {
        int intvalue = 0;

        foreach (char digit in values)
        {
            intvalue += (int)(digit - '0');
            intvalue <<= 1;
        }

        intvalue >>= 1;

        return intvalue;
    }
    private void SetGyroValue(char axis, string values)
    {

    }
    private void SetAccValue(char axis, string values)
    {

    }
    private void SetAngValue(char axis, string values)
    {
        switch (axis)
        {
            case 'X':
                ax = SetStringValuesToFloatValue(values);
                if( !isax )
                {
                    xw = (float)(88 - ax);
                    isax = true;
                }
                ax = xw + ax;
                break;
            case 'Y':
                ay = SetStringValuesToFloatValue(values) * 1;
                if( !isay )
                {
                    yw = (float)(-14 - ay);
                    isay = true;
                }
                ay = yw + ay;
                break;
            case 'Z':
                az = SetStringValuesToFloatValue(values) * 1;
                if( !isaz)
                {
                    zw = (float)(-14 - az);
                    isaz = true;
                }
                az = zw + az;
                break;
        }

        // UnityEngine.Debug.Log(axis + " axis pos value  : " + values);

    }
    private void SetPosValue(char axis, string values)
    {
        switch (axis)
        {
            case 'X':
                px = SetStringValuesToFloatValue(values);
                break;
            case 'Y':
                py = SetStringValuesToFloatValue(values);
                break;
            case 'Z':
                pz = SetStringValuesToFloatValue(values);
                break;
        }

        // UnityEngine.Debug.Log(axis + " axis pos value  : " + values);
    }
    void SetButtonValue(string data)
    {
        UnityEngine.Debug.Log(data);
        switch(data)
        {
            case "000":
                bl = false;
                bo = false;
                br = false;
                break;
            case "001":
                bl = false;
                bo = false;
                br = true;
                break;
            case "010":
                bl = false;
                bo = true;
                br = false;
                break;
            case "011":
                bl = false;
                bo = true;
                br = true;
                break;
            case "100":
                bl = true;
                bo = false;
                br = false;
                break;
            case "101":
                bl = true;
                bo = false;
                br = true;
                break;
            case "110":
                bl = true;
                bo = true;
                br = false;
                break;
            case "111":
                bl = true;
                bo = true;
                br = true;
                break;






        }
    }
    // get measure value
    public Vector3 GetAngValue()
    {
        return new Vector3(ax, ay, az);
    }
    public Vector3 GetPosValue()
    {
        return new Vector3(px, py, pz);
    }
    public bool GetButtonL()
    {
        return bl;
    }
    public bool GetButtonO()
    {
        return bo;
    }
    public bool GetButtonR()
    {
        return br;
    }

}

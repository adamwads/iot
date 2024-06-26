﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Device.Gpio;
using System.Device.Spi;
using System.Threading;
using Iot.Device.Multiplexing;

using Sn74hc595 sr = new(Sn74hc595PinMapping.Complete);

// Uncomment this code to use SPI (and comment the line above)
// SpiConnectionSettings settings = new(0, 0);
// using var spiDevice = SpiDevice.Create(settings);
// using Sn74hc595 sr = new(spiDevice);
CancellationTokenSource cancellationSource = new();
Console.CancelKeyPress += (s, e) =>
{
    e.Cancel = true;
    cancellationSource.Cancel();
};

Console.WriteLine($"Driver for {nameof(Iot.Device.Multiplexing.Sn74hc595)}");
Console.WriteLine($"Register bit length: {sr.BitLength}");
string interfaceType = sr.UsesSpi ? "SPI" : "GPIO";
Console.WriteLine($"Using {interfaceType}");

if (!sr.UsesSpi)
{
    DemonstrateShiftingBits(sr, cancellationSource);
}

DemonstrateShiftingBytes(sr, cancellationSource);
BinaryCounter(sr, cancellationSource);

void DemonstrateShiftingBits(Sn74hc595 sr, CancellationTokenSource cancellationSource)
{
    int delay = 1000;
    sr.ShiftClear();

    Console.WriteLine("Light up three of first four LEDs");
    sr.ShiftBit(1);
    sr.ShiftBit(1);
    sr.ShiftBit(0);
    sr.ShiftBit(1);
    sr.Latch();
    Thread.Sleep(delay);

    sr.ShiftClear();

    Console.WriteLine($"Light up all LEDs, with {nameof(sr.ShiftBit)}");

    for (int i = 0; i < sr.BitLength; i++)
    {
        sr.ShiftBit(1);
    }

    sr.Latch();
    Thread.Sleep(delay);

    sr.ShiftClear();

    Console.WriteLine($"Dim up all LEDs, with {nameof(sr.ShiftBit)}");

    for (int i = 0; i < sr.BitLength; i++)
    {
        sr.ShiftBit(0);
    }

    sr.Latch();
    Thread.Sleep(delay);

    if (IsCanceled(sr, cancellationSource))
    {
        return;
    }
}

void DemonstrateShiftingBytes(Sn74hc595 sr, CancellationTokenSource cancellationSource)
{
    int delay = 1000;
    Console.WriteLine($"Write a set of values with {nameof(sr.ShiftByte)}");
    // this can be specified as ints or binary notation -- its all the same
    var values = new byte[] { 0b1, 23, 56, 127, 128, 170, 0b_1010_1010 };
    foreach (var value in values)
    {
        Console.WriteLine($"Value: {value}");
        sr.ShiftByte(value);
        Thread.Sleep(delay);
        sr.ShiftClear();

        if (IsCanceled(sr, cancellationSource))
        {
            return;
        }
    }

    byte lit = 0b_1111_1111; // 255
    Console.WriteLine($"Write {lit} to each register with {nameof(sr.ShiftByte)}");
    for (int i = 0; i < sr.BitLength / 8; i++)
    {
        sr.ShiftByte(lit);
    }

    Thread.Sleep(delay);

    Console.WriteLine("Output disable");
    sr.OutputEnable = false;
    Thread.Sleep(delay * 2);

    Console.WriteLine("Output enable");
    sr.OutputEnable = true;
    Thread.Sleep(delay * 2);

    Console.WriteLine($"Write 23 then 56 with {nameof(sr.ShiftByte)}");
    sr.ShiftByte(23);
    sr.ShiftByte(56);
    sr.ShiftClear();
}

void BinaryCounter(Sn74hc595 sr, CancellationTokenSource cancellationSource)
{
    Console.WriteLine($"Write 0 through 255");
    for (int i = 0; i < 256; i++)
    {
        sr.ShiftByte((byte)i);
        Thread.Sleep(50);
        sr.ClearStorage();

        if (IsCanceled(sr, cancellationSource))
        {
            return;
        }
    }

    sr.ShiftClear();

    if (sr.BitLength > 8)
    {
        Console.WriteLine($"Write 256 through 4095; pick up the pace");
        for (int i = 256; i < 4096; i++)
        {
            ShiftBytes(sr, i);
            Thread.Sleep(25);
            sr.ClearStorage();

            if (IsCanceled(sr, cancellationSource))
            {
                return;
            }
        }
    }

    sr.ShiftClear();
}

void ShiftBytes(Sn74hc595 sr, int value)
{
    if (sr.BitLength > 32)
    {
        throw new Exception($"{nameof(ShiftBytes)}: bit length must be  8-32.");
    }

    for (int i = (sr.BitLength / 8) - 1; i > 0; i--)
    {
        int shift = i * 8;
        int downShiftedValue = value >> shift;
        sr.ShiftByte((byte)downShiftedValue);
    }

    sr.ShiftByte((byte)value);
}

bool IsCanceled(Sn74hc595 sr, CancellationTokenSource cancellationSource)
{
    if (cancellationSource.IsCancellationRequested)
    {
        sr.ShiftClear();
        return true;
    }

    return false;
}

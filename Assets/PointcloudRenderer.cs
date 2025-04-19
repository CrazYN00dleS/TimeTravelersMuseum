using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEngine;
using UnityEngine.VFX;

public class PointCloudRenderer : MonoBehaviour
{
    Texture2D texColor;
    Texture2D texPosScale;
    VisualEffect vfx;
    uint resolution = 2048;

    public float particleSize = 0.1f;
    bool toUpdate = false;
    uint particleCount = 0;

    public string plyFileName = "4_17_2025.ply"; // file name
    public bool autoLoadOnStart = true;

    private void Start()
    {
        vfx = GetComponent<VisualEffect>();

        if (autoLoadOnStart)
        {
            string path = Path.Combine("D:\\PolycamObject\\GS_scene", plyFileName);

            LoadPLY(path);
        }
    }

    private void Update()
    {
        if (toUpdate)
        {
            toUpdate = false;

            vfx.Reinit();
            vfx.SetUInt(Shader.PropertyToID("ParticleCount"), particleCount);
            vfx.SetTexture(Shader.PropertyToID("TexColor"), texColor);
            vfx.SetTexture(Shader.PropertyToID("TexPosScale"), texPosScale);
            vfx.SetUInt(Shader.PropertyToID("Resolution"), resolution);
        }
    }

    public void LoadPLY(string path)
    {
        if (!File.Exists(path))
        {
            Debug.LogError("File not found: " + path);
            return;
        }

        using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read))
        using (var reader = new BinaryReader(stream))
        {
            // Read header
            List<string> headerLines = new List<string>();
            string line;
            while ((line = ReadAsciiLine(reader)) != null)
            {
                headerLines.Add(line);
                if (line.StartsWith("end_header"))
                    break;
            }

            // Parse header
            int vertexCount = 0;
            bool hasColor = false;
            foreach (string hLine in headerLines)
            {
                if (hLine.StartsWith("element vertex"))
                    vertexCount = int.Parse(hLine.Split()[2]);
                if (hLine.Contains("property uchar red"))
                    hasColor = true;
            }

            // Load vertices
            Vector3[] positions = new Vector3[vertexCount];
            Color[] colors = new Color[vertexCount];
            int floatsPerVertex = 62;

            for (int i = 0; i < vertexCount; i++)
            {
                // Position
                float x = reader.ReadSingle();
                float y = reader.ReadSingle();
                float z = reader.ReadSingle();
                positions[i] = new Vector3(x, y, z);

                // Skip nx, ny, nz (3 floats)
                reader.BaseStream.Seek(3 * sizeof(float), SeekOrigin.Current);

                // f_dc_0, f_dc_1, f_dc_2 ¡ú use as color
                float r = reader.ReadSingle();
                float g = reader.ReadSingle();
                float b = reader.ReadSingle();
                colors[i] = new Color(Mathf.Clamp01(r), Mathf.Clamp01(g), Mathf.Clamp01(b));

                // Skip remaining floats (62 - 9 = 53 floats)
                reader.BaseStream.Seek((floatsPerVertex - 9) * sizeof(float), SeekOrigin.Current);
            }



            SetParticles(positions, colors);
        }
    }

    private string ReadAsciiLine(BinaryReader reader)
    {
        List<byte> bytes = new List<byte>();
        byte b;
        try
        {
            while ((b = reader.ReadByte()) != 10)
            {
                bytes.Add(b);
            }
            return System.Text.Encoding.ASCII.GetString(bytes.ToArray());
        }
        catch
        {
            return null;
        }
    }

    public void SetParticles(Vector3[] positions, Color[] colors)
    {
        texColor = new Texture2D(positions.Length > (int)resolution ? (int)resolution : positions.Length, Mathf.Clamp(positions.Length / (int)resolution, 1, (int)resolution), TextureFormat.RGBAFloat, false);
        texPosScale = new Texture2D(texColor.width, texColor.height, TextureFormat.RGBAFloat, false);
        int texWidth = texColor.width;
        int texHeight = texColor.height;

        for (int y = 0; y < texHeight; y++)
        {
            for (int x = 0; x < texWidth; x++)
            {
                int index = x + y * texWidth;
                if (index >= positions.Length) break;
                texColor.SetPixel(x, y, colors[index]);
                texPosScale.SetPixel(x, y, new Color(positions[index].x, positions[index].y, positions[index].z, particleSize));
            }
        }

        texColor.Apply();
        texPosScale.Apply();
        particleCount = (uint)positions.Length;
        toUpdate = true;
    }
}

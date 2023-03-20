using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public class TextureAssetImporter : AssetPostprocessor
{
	private static readonly HashSet<string> tempIgnoreAssets = new HashSet<string>();

	public const string GameDir = "Assets/Game/";
	public const string UIsDir = "Assets/Game/UIs/";
	public const string ViewDir = "Assets/Game/UIs/View/";
	public const string IconsDir = "Assets/Game/UIs/Icons/";
	public const string FontsDir = "Assets/Game/UIs/Fonts/";
	public const string RawImagesDir = "Assets/Game/UIs/RawImages/";

	public const string ActorDir = "Assets/Game/Actors/";
	public const string ShaderDir = "Assets/Game/Shaders/";

	public const string NoPack = "/nopack/";

	// ָ������ͼƬѹ����ʽ
	private static Dictionary<TextureImporterType, TextureImporterFormat> compressRules =
		new Dictionary<TextureImporterType, TextureImporterFormat>
		{
			{TextureImporterType.NormalMap, TextureImporterFormat.ASTC_8x8}
		};

	//�������ǰ����
	void OnPreprocessTexture()
	{
		var importer = (TextureImporter) assetImporter;
		if (importer.hideFlags == HideFlags.NotEditable) return;
		if (!importer.assetPath.StartsWith("Assets/")) return;
		if (importer.assetPath.StartsWith("Assets/.")) return;
		if (tempIgnoreAssets.Contains(importer.assetPath)) return;

		PreprocessTextureType(importer);
		PreprocessReadable(importer);
		PreprocessFilterMode(importer);
		PreprocessAdvanced(importer);
		PreprocessPlatform(importer);
	}

	void PreprocessTextureType(TextureImporter importer)
	{
		var assetPath = importer.assetPath;
		if (assetPath.StartsWith(RawImagesDir))
			importer.textureType = TextureImporterType.Default;
		else if (assetPath.StartsWith(IconsDir)
		         || assetPath.StartsWith(FontsDir)
		         || assetPath.StartsWith(ViewDir))
			importer.textureType = TextureImporterType.Sprite;
	}

	void PreprocessReadable(TextureImporter importer)
	{
		if (importer.assetPath.StartsWith(FontsDir))
			importer.isReadable = true;
		else
			importer.isReadable = false;
	}

	void PreprocessFilterMode(TextureImporter importer)
	{
		if (importer.textureType == TextureImporterType.Sprite)
			importer.filterMode = FilterMode.Bilinear;
	}

	void PreprocessAdvanced(TextureImporter importer)
	{
		if (importer.assetPath.StartsWith(RawImagesDir))
		{
			importer.alphaIsTransparency = false;
			importer.npotScale = TextureImporterNPOTScale.None;
			importer.wrapMode = TextureWrapMode.Clamp;
		}
	}

	void PreprocessPlatform(TextureImporter importer)
	{
		//������SpriteAtlas��ѹ������
		if (importer.textureType == TextureImporterType.Sprite)
			return;
		var noCompress = importer.assetPath.Contains("/Editor/")
		                 || importer.textureType == TextureImporterType.GUI
		                 || importer.textureType == TextureImporterType.SingleChannel;
		var defaultSettings = importer.GetDefaultPlatformTextureSettings();
		if (noCompress || defaultSettings.format == TextureImporterFormat.Alpha8)
		{
			importer.ClearPlatformTextureSettings("Standalone");
			importer.ClearPlatformTextureSettings("Android");
			importer.ClearPlatformTextureSettings("iPhone");
			if (noCompress)
				importer.textureCompression = TextureImporterCompression.Uncompressed;
		}
		else
		{
			importer.textureCompression = TextureImporterCompression.CompressedHQ;
			importer.SetPlatformTextureSettings(defaultSettings);

			var winSettings = importer.GetPlatformTextureSettings("Standalone");
			if (PreprocessUnifyPlatform(importer, winSettings)) //pcƽ̨����������ߴ�
				importer.SetPlatformTextureSettings(winSettings);
			var iosSettings = importer.GetPlatformTextureSettings("iPhone");
			if (PreprocessUnifyPlatform(importer, iosSettings) || PreprocessTextureMaxSize(importer, iosSettings))
				importer.SetPlatformTextureSettings(iosSettings);
			var andSettings = importer.GetPlatformTextureSettings("Android");
			if (PreprocessUnifyPlatform(importer, andSettings) || PreprocessTextureMaxSize(importer, andSettings))
				importer.SetPlatformTextureSettings(andSettings);
		}
	}

	//������������ָ��ѹ����ʽ
	static TextureImporterFormat GetCompressType(TextureImporterType type)
	{
		if (compressRules.TryGetValue(type, out var format))
			return format;
		return TextureImporterFormat.ASTC_6x6; //Ĭ��ѹ����ʽ
	}

	bool PreprocessUnifyPlatform(TextureImporter importer, TextureImporterPlatformSettings settings)
	{
		var format = GetCompressType(importer.textureType);
		return false;
	}

	bool PreprocessTextureMaxSize(TextureImporter importer, TextureImporterPlatformSettings settings)
	{
		var dirty = importer.textureType == TextureImporterType.NormalMap
		            || importer.assetPath.StartsWith(ActorDir);
		if (dirty)
			dirty = settings.maxTextureSize != 1024;
		if (dirty)
			settings.maxTextureSize = 1024;
		return dirty;
	}


	//�������ʱ����
	void OnPostprocessTexture(Texture2D texture)
	{
		var importer = (TextureImporter) assetImporter;
		if (tempIgnoreAssets.Contains(importer.assetPath)) return;
		if (!importer.assetPath.StartsWith("Assets/")) return;
		if (importer.assetPath.StartsWith("Assets/.")) return;

		PostprocessResize2POT(importer, texture);
		PostprocessNoPackTips(importer, texture);
	}

	void PostprocessResize2POT(TextureImporter importer, Texture2D texture)
	{
		//RawImage��Դ����ҪPOT
		if (string.IsNullOrEmpty(importer.spritePackingTag))
			return;
		var w_mod = texture.width % 4;
		var h_mod = texture.height % 4;
		if (w_mod == 0 && h_mod == 0)
			return;
		tempIgnoreAssets.Add(importer.assetPath);
		importer.isReadable = true;
		importer.SaveAndReimport();
		{
			var new_texture = new Texture2D(texture.width + 4 - w_mod, texture.height + 4 - h_mod);
			for (int x = 0, wlen = new_texture.width; x < wlen; x++)
			for (int y = 0, hlen = new_texture.height; y < hlen; y++)
				new_texture.SetPixel(x, y, new Color(0, 0, 0, 0));
			new_texture.SetPixels32(w_mod > 0 ? 1 : 0, h_mod > 0 ? 1 : 0, texture.width, texture.height,
				texture.GetPixels32());
			File.WriteAllBytes(importer.assetPath, new_texture.EncodeToPNG());
		}
		importer.isReadable = false;
		importer.SaveAndReimport();
		tempIgnoreAssets.Remove(importer.assetPath);
	}

	void PostprocessNoPackTips(TextureImporter importer, Texture2D texture)
	{
		if (importer.textureType != TextureImporterType.Sprite
		    || importer.assetPath.ToLower().Contains(NoPack))
			return;
		if (texture.width > 1024 || texture.height > 1024)
			Debug.LogError(
				$"{importer.assetPath} <color=yellow>ͼƬ��������1024</color>, �뿼�Ƿŵ� nopack �� RawImages �ļ�����!");
		else if (texture.width * texture.height > 400 * 400)
			Debug.LogError(
				$"{importer.assetPath} <color=yellow>ͼƬ�ߴ����400*400</color>, �뿼�Ƿŵ� nopack �� RawImages �ļ�����!");
	}
}
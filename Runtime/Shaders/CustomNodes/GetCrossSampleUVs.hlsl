void GetCrossSampleUVs_float(float4 UV, float2 TexelSize, float OffsetMultiplier, 
    out float2 UVOriginal, out float2 UVTopRight, out float2 UVBottomLeft, out float2 UVTopLeft, out float2 UVBottomRight) {
        UVOriginal = UV;
        UVTopRight = UV.xy + float2(TexelSize.x, TexelSize.y) * OffsetMultiplier;
        UVBottomLeft = UV.xy - float2(TexelSize.x, TexelSize.y) * OffsetMultiplier;
        UVTopLeft = UV.xy + float2(-TexelSize.x * OffsetMultiplier, TexelSize.y * OffsetMultiplier);
        UVBottomRight = UV.xy + float2(TexelSize.x * OffsetMultiplier, -TexelSize.y * OffsetMultiplier);
}
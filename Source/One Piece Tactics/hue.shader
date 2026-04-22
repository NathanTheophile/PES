shader_type canvas_item;
render_mode unshaded;

uniform float Shift_Hue;
uniform float Shift_Sat;
uniform float Shift_Val;

// 0.88, 0.35, 0.15 - F
// 0.15, 0.25, -0.3 - E
// 0.39, 0.45, 0.11 - W
// 0.71, 0.15, 0.18 - A
// 0.0, 0.0, 0.0 - N

vec3 hsv2rgb(vec3 c)
{
	vec3 C = c;
	C.x+=Shift_Hue;
	C.y+=Shift_Sat;
	C.z+=Shift_Val;
	vec4 K = vec4(1.0, 2.0 / 3.0, 1.0 / 3.0, 3.0);
	vec3 p = abs(fract(C.xxx + K.xyz) * 6.0 - K.www);
	return C.z * mix(K.xxx, clamp(p - K.xxx, 0.0, 1.0), C.y);
}

vec3 rgb2hsv(vec3 c)
{
	vec4 K = vec4(0.0, -1.0 / 3.0, 2.0 / 3.0, -1.0);
	vec4 p = mix(vec4(c.bg, K.wz), vec4(c.gb, K.xy), step(c.b, c.g));
	vec4 q = mix(vec4(p.xyw, c.r), vec4(c.r, p.yzx), step(p.x, c.r));

	float d = q.x - min(q.w, q.y);
	float e = 1.0e-10;
	return vec3(abs(q.z + (q.w - q.y) / (6.0 * d + e)), d / (q.x + e), q.x);
}

void fragment() {
	COLOR=texture(TEXTURE,UV);
	vec3 rgb = COLOR.rgb;
	vec3 hsv = rgb2hsv(rgb);
	vec3 result = hsv2rgb(hsv);
	COLOR.rgb=result;
}
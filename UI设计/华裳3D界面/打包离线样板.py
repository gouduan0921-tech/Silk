from pathlib import Path
import re, base64
root=Path(__file__).resolve().parent
html=(root/'index.html').read_text()
css=(root/'style.css').read_text()
js=(root/'app.js').read_text()
for path in (root/'素材').glob('*.png'):
    key='素材/'+path.name
    js=js.replace(key,'data:image/png;base64,'+base64.b64encode(path.read_bytes()).decode())
html=html.replace('<link rel="stylesheet" href="style.css">','<style>'+css+'</style>').replace('<script src="app.js"></script>','<script>'+js+'</script>')
(root/'直接打开.html').write_text(html)
print('已生成直接打开.html，场景与操作全部内置。')

"""Read ELF metadata without executing Linux binaries; useful when cross publishing."""
from pathlib import Path
import struct,sys
root=Path(sys.argv[1])
for path in sorted(root.glob('*')):
 if not path.is_file():continue
 data=path.read_bytes()
 if data[:6]!=b'\x7fELF\x02\x01':continue
 header=struct.unpack_from('<16sHHIQQQIHHHHHH',data)
 sections=[struct.unpack_from('<IIQQQQIIQQ',data,header[6]+i*header[11]) for i in range(header[12])]
 needed=[]
 for section in sections:
  if section[1]!=6:continue
  strings=sections[section[6]]
  for offset in range(section[4],section[4]+section[5],16):
   tag,value=struct.unpack_from('<qQ',data,offset)
   if tag==1:
    start=strings[4]+value;needed.append(data[start:data.index(b'\0',start)].decode())
 print(path.name+': '+', '.join(needed))

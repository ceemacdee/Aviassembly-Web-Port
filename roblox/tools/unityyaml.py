import re, os, glob, functools
ROOT=__import__('os').environ.get('AVIASSEMBLY_ASSETS','/home/user/extract/NewAvi/ExportedProject/Assets')
DOC_RE=re.compile(r'^--- !u!(\d+) &(-?\d+)(?: stripped)?$',re.M)

def parse_scalar(v):
    v=v.strip()
    if v.startswith('{') and v.endswith('}'):
        out={}
        for part in re.findall(r'(\w+): ([^,}]*)',v):
            out[part[0]]=parse_scalar(part[1])
        return out
    if v in ('',): return None
    try:
        if re.fullmatch(r'-?\d+',v): return int(v)
        return float(v)
    except ValueError:
        return v.strip("'\"")

def parse_block(lines, start, indent):
    """Parse YAML-ish block (Unity subset). Returns (value, next_index)."""
    # list?
    i=start
    if i<len(lines) and lines[i].startswith(' '*indent+'- '):
        arr=[]
        while i<len(lines) and lines[i].startswith(' '*indent+'- '):
            first=lines[i][indent+2:]
            m=re.match(r'^(\w[\w ]*?): ?(.*)$',first)
            if m and not first.startswith('{'):
                # mapping item: rewrite line as indent+2 mapping
                sub=[' '*(indent+2)+first]
                j=i+1
                while j<len(lines) and (lines[j].startswith(' '*(indent+2)) and not lines[j].startswith(' '*indent+'- ')):
                    sub.append(lines[j]); j+=1
                val,_=parse_block(sub,0,indent+2)
                arr.append(val); i=j
            else:
                arr.append(parse_scalar(first)); i+=1
        return arr,i
    d={}
    while i<len(lines):
        line=lines[i]
        if not line.strip(): i+=1; continue
        cur=len(line)-len(line.lstrip(' '))
        if cur<indent: break
        if cur>indent: i+=1; continue
        m=re.match(r'^\s*([^:]+?):(?: (.*))?$',line)
        if not m: i+=1; continue
        key,val=m.group(1),m.group(2)
        if val is None or val=='':
            # nested
            if i+1<len(lines):
                nl=lines[i+1]; ni=len(nl)-len(nl.lstrip(' '))
                if nl.lstrip().startswith('- ') and ni>=indent:
                    v,i=parse_block(lines,i+1,ni); d[key]=v; continue
                if ni>indent:
                    v,i=parse_block(lines,i+1,ni); d[key]=v; continue
            d[key]=None; i+=1
        else:
            if val=='[]': d[key]=[]
            elif val=='{}': d[key]={}
            else: d[key]=parse_scalar(val)
            i+=1
    return d,i

@functools.lru_cache(None)
def load(path):
    text=open(path,encoding='utf8').read()
    docs={}
    ms=list(DOC_RE.finditer(text))
    for k,m in enumerate(ms):
        body=text[m.end():ms[k+1].start() if k+1<len(ms) else len(text)]
        lines=body.split('\n')[1:]
        typ=lines[0].rstrip(':') if lines else ''
        val,_=parse_block(lines[1:],0,2)
        docs[m.group(2)]=(int(m.group(1)),typ,val)
    return docs

@functools.lru_cache(None)
def guid_map():
    g={}
    for meta in glob.glob(ROOT+'/**/*.meta',recursive=True):
        t=open(meta,encoding='utf8',errors='ignore').read(400)
        m=re.search(r'guid: (\w+)',t)
        if m: g[m.group(1)]=meta[:-5]
    return g

def script_name(guid):
    p=guid_map().get(guid)
    return os.path.basename(p)[:-3] if p and p.endswith('.cs') else None

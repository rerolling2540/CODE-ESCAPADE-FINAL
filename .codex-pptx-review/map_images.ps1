Add-Type -AssemblyName System.IO.Compression.FileSystem
$zip=[System.IO.Compression.ZipFile]::OpenRead('C:\Users\ACER\Downloads\Presentation1 (2).pptx')
foreach($s in @(1,2,11,12,13,14,15)){
  $relsE=$zip.GetEntry("ppt/slides/_rels/slide$s.xml.rels"); $xmlE=$zip.GetEntry("ppt/slides/slide$s.xml");
  if(!$relsE -or !$xmlE){continue}
  $reader=[IO.StreamReader]::new($relsE.Open());$rels=[xml]$reader.ReadToEnd();$reader.Dispose()
  $reader=[IO.StreamReader]::new($xmlE.Open());$doc=[xml]$reader.ReadToEnd();$reader.Dispose()
  $map=@{}; foreach($rel in $rels.Relationships.Relationship){$map[$rel.Id]=$rel.Target}
  $ns=[System.Xml.XmlNamespaceManager]::new($doc.NameTable);$ns.AddNamespace('p','http://schemas.openxmlformats.org/presentationml/2006/main');$ns.AddNamespace('a','http://schemas.openxmlformats.org/drawingml/2006/main');$ns.AddNamespace('r','http://schemas.openxmlformats.org/officeDocument/2006/relationships')
  foreach($pic in $doc.SelectNodes('//p:pic',$ns)){$name=$pic.SelectSingleNode('./p:nvPicPr/p:cNvPr',$ns).GetAttribute('name');$blip=$pic.SelectSingleNode('.//a:blip',$ns);$rid=$blip.GetAttribute('embed','http://schemas.openxmlformats.org/officeDocument/2006/relationships');Write-Output ("slide$s $name => $($map[$rid])")}
}
$zip.Dispose()

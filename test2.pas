program Test2;
var
  ch: char;
begin
  case ch of
    'a': writeln('A');
    'k: writeln('K');
    'i: writeln('I');
    'b'...'d': writeln('B-D'); 
    else
      writeln('Другая буква');  
      writeln('Неизвестно');   
  end;

  if ch in ['a'..'z'] 
    writeln('Это буква');
end.

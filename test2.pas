program Test2;
var
  ch: char;
begin
  case ch of
    'a': writeln('A');
    k: writeln('K');
    i: writeln('I');
    'b'...'d': writeln('B-D');
  end;
end.
class Table {
  public Header: Header;
  public Rows: Row[];
  public HasHeader() { return this.Header?.IsContent ?? false; }

  constructor(HeaderRow?: Header, Data?: Row[]) {
    this.Header = HeaderRow;
    this.Header.Table = this;
    this.Rows = Data ?? [];
  }

  /**
   * 逐字符的解析完整CSV内容
   * @param Char1
   */
  public NextChar(Char1: string) {
    if (this.Rows?.length === 0) {
      const Next = this.Header.NextChar(Char1);
      if (Next !== null) this.Rows.push(Next);
    }
    else {
      const Next = this.Rows[this.Rows.length - 1].NextChar(Char1);
      if (Next !== null) {
        this.Rows.push(Next);
      }
    }
  }
  public NextCharSkipHeader(Chat1: string) {

  }
  public ReplaceChar(Char1: string) {
    const Next = this.Header.ReplaceChar(Char1);
  }


  /**转换为CSV */
  public ToString() {
    return `${this.Header.ToString()}${this.Rows.join("\n\r")}`;
  }
  /**转换为字典类型 */
  public ToObject(): any {
    return null;
  }
  /**
   * 配置Header
   * @param HeaderText
   * @param Converters
   */
  public ApplyHeader(HeaderText: string[], Converters?: Converter[]) {
    if (this.Header == null) {
      this.Header = new Header(this, true, HeaderText);
    }
    else
      this.Header.AddContents(HeaderText);
  }
  /**
   * 添加一行
   * @param DataRow
   */
  public AddRow(DataRow: any[]) {
    const _Row = new Row(this);
    _Row.AddContents(DataRow);
    this.Rows.push(_Row);
  }




  *[Symbol.iterator]() {
    for (let Row of this.Rows) {
      yield Row;
    }
  }

}
class Row {
  public readonly Fields: Field[] = [];
  public Table: Table;
  constructor(Table?: Table, Vals?: any[], Texts?: string[]) {
    this.Table = Table;
    if (Vals === null) this.AddContents(Vals);
    else if (Texts !== null) this.AddTexts(Texts);
  }
  /**
   * 逐字符的解析完整CSV内容
   * @param Char1
   */
  public NextChar(Char1: string): Row {
    const Next = this.Fields[this.Fields.length - 1].NextChar(Char1);
    if (Next.Eof) {
      const NR = new Row(this.Table);
      NR.Fields.push(Next.Field);
      return NR;
    }
    else if (Next.Field != null) {
      Next.Field.ColIndex = this.Fields.length;
      this.Fields.push(Next.Field);
    }
    return null;
  }
  protected _rp_index: number = 0;
  public ReplaceChar(Char1: string): boolean {
    if (this._rp_index < this.Fields.length) {
      const res = this.Fields[this._rp_index].ReplaceChar(Char1);
      if (res === 1) {
        this._rp_index++;
      }
      else if (res === 2) {
        this._rp_index = 0;
        return true;
      }
      else return false;
    }
    else {
      const NewF = new Field(this.Table);
      NewF.ColIndex = this.Fields.length;
      this.Fields.push(NewF);
      const Res = NewF.NextChar(Char1);
      //if (Res.)
    }
    return false;
  }

  /**转换为CSV */
  public ToString(): string {
    return `${this.Fields.join(",")}`;
  }
  /**导出到列表 */
  public ToObject(): any[] {
    const Arr = [];
    this.Fields.forEach(E => Arr.push(E.ToValue()));
    return Arr;
  }
  /**
   * 从CSV内容添加多列
   * @param Texts
   */
  public AddTexts(Texts: string[]) {
    Texts.forEach(E => this.AddText(E));
  }
  /**
   * 从CSV内容添加一列
   * @param Text
   */
  public AddText(Text: string) {
    const Fd = new Field(this.Table);
    Fd.SetText(Text);
    this.Fields.push(Fd);
  }
  /**
   * 从一组值添加多列
   * @param Text
   */
  public AddContents(Vals: any[]) {
    Vals.forEach(E => {
      this.AddContent(E);
    });
  }
  /**
   * 从值添加一列
   * @param Text
   */
  public AddContent(Val: any) {
    const Fd = new Field(this.Table);
    Fd.SetValue(Val);
    this.Fields.push(Fd);
  }

  *[Symbol.iterator]() {
    for (let Field of this.Fields) {
      yield Field;
    }
  }
}
class Field {
  protected _Chars: string[];
  protected Text: string = null;
  protected Enclosed: boolean = true;
  public Table: Table;
  public ColIndex: number;
  public Header(): Field {
    if (this.Table.HasHeader() && this.Table.Rows.length > 0) {
      return this.Table.Header.PeekHeader(this.ColIndex);
    }
    return null;
  }
  /**
   * 逐字符的解析完整CSV内容
   * @param Char1
   */
  public NextChar(Char1: string): Eof {
    const Eof = { Eof: false, Field: null };
    if (Char1 === "\"") this.Enclosed = !this.Enclosed;
    if (this.Enclosed) {
      if (Char1 === ",") {
        Eof.Field = new Field(this.Table);
        return Eof;
      }
      else if (Char1 === "\r" && this._Chars.length > 0 && this._Chars[this._Chars.length - 1] === "\n") {
        Eof.Eof = true;
        Eof.Field = new Field(this.Table);
        this._Chars.slice(this._Chars.length - 1, 1);
        return Eof;
      }
    }
    else
      this._Chars.push(Char1);
    return null;
  }
  protected _rp_index: number = 0;
  /**
   * 逐字符替换内容
   * @param Char1
   * @returns 0=nothing 1=next field 2=next row
   */
  public ReplaceChar(Char1: string): number {
    if (this._rp_index === 0) { this._Chars = []; this.Enclosed = true; this.Text = null; }
    if (Char1 === "\"") this.Enclosed = !this.Enclosed;
    if (this.Enclosed) {
      if (Char1 === ",") {
        this._rp_index = 0;
        return 1;
      }
      else if (Char1 === "\r" && this._Chars.length > 0 && this._Chars[this._Chars.length - 1] === "\n") {
        this._rp_index = 0;
        this._Chars.slice(this._Chars.length - 1, 1);
        return 2;
      }
    }
    else
      this._Chars.push(Char1);
    this._rp_index++;
    return 0;
  }
  /**转换为CSV内容 */
  public ToString() {
    if (this.Text !== null) return this.Text;
    const NewChars = [];
    let Enc = false;
    this._Chars.forEach(E => {
      NewChars.push(E);
      if (E === "\"") NewChars.push("\"");
    });
    const Str = NewChars.join("");
    this.Text = Str.indexOf("\n\r") != -1 || Str.indexOf(",") != -1 ? `"${Str}"` : Str;
    return this.Text;
  }
  /**转换为值 */
  public ToValue(): any {
    return this.Table.Header.PeekConverter(this.ColIndex).From(this._Chars.join(""));
  }
  /**
   * 输入值
   * @param Value
   */
  public SetValue(Value: any) {
    this.SetText(this.Table.Header.PeekConverter(this.ColIndex).To(Value));
  }
  /**
   * 输入CSV Field内容
   * @param Text
   */
  public SetText(Text: string) {
    this._Chars = [];
    this.Text = Text;
    this.Enclosed = true;
    for (let i = 0; i < Text.length; i++) {
      if (Text[i] == "\"") this.Enclosed = !this.Enclosed;
      if (this.Enclosed) {

      }
      else {
        this._Chars.push(Text[i]);
      }
    }
  }


  constructor(Table: Table) {
    this.Table = Table;
  }
  //public static FromCSVText(Row: Row, Text: string): Field {
  //  const F = new Field(Row.Table);
  //  F.SetText(Text);
  //  return F;
  //}
  //public static FormObject(Row: Row, Value: any): Field {
  //  const F = new Field(Row.Table);
  //  F.SetValue(Value);
  //  return F;
  //}


}

interface Converter {
  To(any): string;
  From(string): any;
}
class Header extends Row {
  protected Converters: Converter[];

  public IsContent: boolean;

  public static FullbackConverter(): Converter {
    return {
      To: Header.FullbackToTextConverter,
      From: Header.FullbackFromTextConverter,
    }
  }
  private static FullbackToTextConverter(Value: any): string {
    if (Value instanceof Date) {
      return (Value as Date).toISOString();
    }
    return Value.ToString();
  }
  private static FullbackFromTextConverter(Text: string): string { return Text; }

  public PeekHeader(ColIndex: number) {
    if (this.Fields.length > 0 && ColIndex < this.Fields.length) {
      return this.Fields[ColIndex];
    }
    return null;
  }
  public PeekConverter(ColIndex: number): Converter {
    return this.Converters[ColIndex];
  }

  public AddContents(Texts: string[]) {
    this.AddTexts(Texts);
  }
  public AddContent(Text: string) {
    this.AddText(Text);
  }
  public AddTexts(Texts: string[], Converters?: Converter[]) {
    Converters = Converters ?? [];
    for (let i = 0; i < Texts.length; i++) {
      const C = i < Converters.length ? Converters[i] : Header.FullbackConverter();
      this.AddText(Texts[i], C)
    }
  }
  public AddText(Text: string, Converter?: Converter) {
    const Fd = new Field(this.Table);
    Fd.SetText(Text);
    this.Converters.push(Converter ?? Header.FullbackConverter());
    this.Fields.push(Fd);
  }

  public ToString(): string {
    return this.IsContent ? "" : `${this.Fields.join("\n\r")}\n\r`;
  }

  public NextChar(Char1: string) {
    const Next = this.Fields[this.Fields.length - 1].NextChar(Char1);
    if (Next.Eof) {
      this.Converters = []
      let i = 0;
      this.Fields.forEach(E => {
        E.ColIndex = i++;
        this.Converters.push(Header.FullbackConverter());
      });
      return new Row(this.Table);
    }
    else if (Next.Field != null) {
      const HT = Next.Field.ToString();
      if (HT === '' && this.Fields.length > 0) {
        Next.Field.SetText(this.Fields[this.Fields.length].ToString());
      }
      this.Fields.push(Next.Field);
    }
    return null;
  }

  constructor(Table?: Table, IsContent?: boolean, HeaderTexts?: string[], Converters?: Converter[]) {
    super(Table);
    this.Converters = Converters ?? [];
    this.IsContent = IsContent ?? false;
    if (HeaderTexts !== null) {
      this.AddContents(HeaderTexts);
    }
  }


}

class CSV {
  public static FromCSV(CSVText: string, HasHeader: boolean): Table {
    const Tb = new Table(null, null);
    for (const C of CSVText) {
      Tb.NextChar(C);
    }
    return Tb;
  }

  public static ToCSV(Table: Table): string {
    return Table.ToString();
  }

  public static readonly MIMEType: string = "text/csv";
}

interface Eof {
  Eof: boolean;
  Field?: Field | null;
  Row?: Row | null;
  LastIndex?: number;
}

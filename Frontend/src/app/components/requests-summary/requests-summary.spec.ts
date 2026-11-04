import { ComponentFixture, TestBed } from '@angular/core/testing';
import { RequestsSummary } from './requests-summary';

describe('RequestsSummary', () => {
  let component: RequestsSummary;
  let fixture: ComponentFixture<RequestsSummary>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [RequestsSummary],
    }).compileComponents();

    fixture = TestBed.createComponent(RequestsSummary);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});

import { ComponentFixture, TestBed } from '@angular/core/testing';
import { RequestsFilters } from './requests-filters';

describe('RequestsFilters', () => {
  let component: RequestsFilters;
  let fixture: ComponentFixture<RequestsFilters>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [RequestsFilters],
    }).compileComponents();

    fixture = TestBed.createComponent(RequestsFilters);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});

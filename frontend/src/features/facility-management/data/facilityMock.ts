import type { Facility } from '../../../models/facility'

export const mockFacilities: Facility[] = [
  {
    facilityId: 'facility-001',
    name: 'Thu Duc Central',
    address: 'Thu Duc City, Ho Chi Minh City',
    contactInfo: '0901 111 111',
    description:
      'Central self-storage facility in Thu Duc.',
    status: 'ACTIVE',
  },
  {
    facilityId: 'facility-002',
    name: 'Binh Thanh Hub',
    address:
      'Binh Thanh District, Ho Chi Minh City',
    contactInfo: '0902 222 222',
    description:
      'Storage facility serving Binh Thanh.',
    status: 'ACTIVE',
  },
  {
    facilityId: 'facility-003',
    name: 'District 7 Storage',
    address:
      'District 7, Ho Chi Minh City',
    contactInfo: '0903 333 333',
    description:
      'Storage facility in District 7.',
    status: 'ACTIVE',
  },
  {
    facilityId: 'facility-004',
    name: 'Tan Binh Depot',
    address:
      'Tan Binh District, Ho Chi Minh City',
    contactInfo: '0904 444 444',
    description:
      'Storage facility near Tan Binh.',
    status: 'ACTIVE',
  },
  {
    facilityId: 'facility-005',
    name: 'Bien Hoa Storage',
    address: 'Bien Hoa, Dong Nai',
    contactInfo: '0905 555 555',
    description:
      'Storage facility in Bien Hoa.',
    status: 'ACTIVE',
  },
  {
    facilityId: 'facility-006',
    name: 'District 1 Storage',
    address:
      'District 1, Ho Chi Minh City',
    contactInfo: '0906 666 666',
    description:
      'Central-city storage facility.',
    status: 'INACTIVE',
  },
]